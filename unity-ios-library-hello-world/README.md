## Unity iOS Hello World Plugin

Reusable iOS native plugin (Swift) for Unity. Provides a minimal “Hello World” integration and a foundation to add native features later (permissions, audio, camera, etc.).

References:
- `unity-swift` (base pattern: Swift + ObjC++ bridge + postprocess): https://github.com/Robert-96/unity-swift
- Tutorial Swift + Unity via `_cdecl` (alternativa): https://blog.lslabs.dev/posts/native_ios_unity
- Guia geral criando plugin iOS para Unity em Swift: https://medium.com/@f_yuki/create-an-ios-native-plugin-for-unity-using-swift-bc27e3634339

---

### 1) Como funciona a integração Unity ↔ iOS

- C# chama funções nativas via `DllImport("__Internal")`.
- O bridge ObjC++ (`.mm`) expõe funções C-callable que:
  - Chamam código Swift (ex.: `HelloWorld.sayHello()`),
  - Ou chamam `UnitySendMessage` para enviar callbacks de volta ao Unity.
- Um script de pós-processamento configura o projeto Xcode gerado pelo Unity:
  - Habilita `DEFINES_MODULE` no `UnityFramework`,
  - Define `MODULEMAP_FILE` (usando `UnityFramework.modulemap`),
  - Marca headers do Unity como públicos (ex.: `UnityInterface.h`),
  - Garante embedding das Swift stdlibs no app target.
- Resultado: você pode depurar Swift no Xcode (breakpoints) e orquestrar ida/volta entre Unity e iOS nativo.

---

### 2) Como esse pacote facilita sua vida

- Estrutura pronta (plugin reutilizável) com:
  - Swift nativo,
  - Ponte ObjC++,
  - Wrapper C# seguro (inclui liberação de memória de strings),
  - Pós-processamento Xcode robusto (procura `modulemap` em `Assets` e `Packages`).
- Zero configuração manual no Xcode sempre que exportar o iOS do Unity.
- Escalável: adicione novos métodos Swift/bridge/C# sem mudar arquitetura.
- Compatível com debug no Xcode e com callbacks ao Unity via `UnitySendMessage`.
- Baseada nas práticas do `unity-swift` (modulemap + headers públicos) para minimizar “undefined symbol” e erros de linking.

---

### 3) O que significa cada arquivo e por que existe

Estrutura principal:
```
unity-ios-library-hello-world/
├── package.json
├── README.md
├── Runtime/
│   └── HelloWorldPlugin.cs
└── Plugins/
    └── iOS/
        ├── Source/
        │   ├── HelloWorld.swift
        │   ├── HelloWorldBridge.mm
        │   └── UnityFramework.modulemap
        └── Editor/
            └── HelloWorldPostProcess.cs
```

- `package.json`: metadados do pacote (permite uso via Package Manager ou copiando pasta).
- `Runtime/HelloWorldPlugin.cs`:
  - API pública em C#.
  - `GetMessage()` chama nativo e faz marshaling seguro de string,
  - `SendToUnity(go, method, message)` aciona `UnitySendMessage` do lado nativo.
  - Usa `#if UNITY_IOS && !UNITY_EDITOR` para evitar chamada nativa no Editor.
- `Plugins/iOS/Source/HelloWorld.swift`:
  - Lógica nativa Swift.
  - `sayHello()` retorna a string “Hello from Swift!”,
  - `sendMessageToUnity(...)` invoca `UnitySendMessage`.
- `Plugins/iOS/Source/HelloWorldBridge.mm`:
  - Ponte C-callable usada pelo `DllImport` do C#.
  - Expõe `helloWorldGetMessage()`, `helloWorldFreeString(...)` e `helloWorldSendToUnity(...)`.
  - Inclui `<UnityFramework/UnityFramework-Swift.h>` (gerado quando `DEFINES_MODULE=YES`) e `UnityInterface.h`.
- `Plugins/iOS/Source/UnityFramework.modulemap`:
  - Permite que Swift importe símbolos públicos do `UnityFramework` (incluindo `UnityInterface.h`).
- `Plugins/iOS/Editor/HelloWorldPostProcess.cs`:
  - Pós-processa o Xcode:
    - `DEFINES_MODULE=YES` e `MODULEMAP_FILE` no `UnityFramework`,
    - Copia `UnityFramework.modulemap` para o build se precisar,
    - Marca headers Unity como “Public”,
    - Seta `ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES=YES` no target app.

---

### Como usar (projeto consumidor)

1) Requisitos
- Unity com iOS Build Support,
- Scripting Backend: IL2CPP, Arquitetura: ARM64,
- Xcode instalado.

2) Instalação
- Opção A: copiar `unity-ios-library-hello-world/` para o repositório e mover seu conteúdo para dentro do seu projeto Unity, respeitando estas localizações:
  - `Runtime/` e `Plugins/` devem ficar sob `Assets/` (ex.: `Assets/UnityIOSHelloWorld/...`), ou
- Opção B: via Package Manager → “Add package from disk” e selecione `unity-ios-library-hello-world/package.json`. Se fizer via UPM, garanta que o pós-processamento encontre o `modulemap` (este pacote já tenta localizar em `Assets` e `Packages`).

3) Código de exemplo
```csharp
using UnityEngine;
using Ursula.IOS.HelloWorld;

public class HelloWorldExample : MonoBehaviour
{
	void Start()
	{
		// 1) Mensagem do nativo (IDA: C# -> Bridge -> Swift)
		string msg = HelloWorldPlugin.GetMessage();
		Debug.Log("From Swift: " + msg);

		// 2) Callback para Unity (VOLTA: Swift -> UnitySendMessage)
		// Configure o GameObject e método alvo para receber a mensagem
		HelloWorldPlugin.SendToUnity(gameObject.name, "OnNativeMessage", "Ping from C# through native");
	}

	// Será chamado quando o nativo usar UnitySendMessage(gameObject, "OnNativeMessage", ...)
	public void OnNativeMessage(string payload)
	{
		Debug.Log("OnNativeMessage: " + payload);
	}
}
```

4) Build iOS
- File → Build Settings → iOS → Player Settings:
  - Scripting Backend = IL2CPP,
  - Architecture = ARM64.
- Build and Run.
- O pós-processamento configurará o Xcode automaticamente.

---

### Ações que você deve tomar depois

- Criar uma cena com o script de exemplo acima e adicionar ao Build Settings.
- Fazer Build para iOS e abrir no Xcode.
- Em Xcode:
  - Confirmar que o target `UnityFramework` tem `DEFINES_MODULE=YES` e `MODULEMAP_FILE` apontando para `UnityFramework/UnityFramework.modulemap`,
  - Headers Unity relevantes como “Public”,
  - `ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES=YES` no target app (`Unity-iPhone`),
  - Definir Team/Signing para rodar em device (ou usar simulador).
- Rodar e verificar no console do Xcode:
  - `Hello from Swift!` (resultado do `GetMessage()`),
  - Mensagens do `UnitySendMessage` chegando no método `OnNativeMessage`.

---

### Notas

- No Editor, o plugin retorna stubs (não chama nativo). Use build iOS para testar o Swift.
- Se o pós-processamento não encontrar o `modulemap`, ajuste caminhos do pacote ou mova o plugin para `Assets/Plugins/iOS/...`.
- Para evoluir (permissões, câmera, áudio, speech), adicione métodos Swift e exponha via bridge/C# seguindo o mesmo padrão.


