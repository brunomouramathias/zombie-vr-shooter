# Gerando o APK Android

## Requisitos

- Unity 6000.5.9f1.
- Android Build Support instalado pelo Unity Hub, incluindo:
  - Android SDK e NDK
  - OpenJDK
- Um dispositivo com Android 8.0 (API 26) ou superior para testar.

## Passos

1. Abra o projeto no Unity 6000.5.9f1.
2. Abra `Assets/Scenes/Zombie.unity`.
3. Vá em **File > Build Settings**.
4. Selecione **Android** e clique em **Switch Platform**.
5. Confirme que a lista de cenas contém apenas `Assets/Scenes/Zombie.unity`.
6. Clique em **Build** e escolha o caminho de saída.

O projeto também inclui um comando de editor que gera a mesma cena em
`Builds/ZombieVR.apk`:

- **Tools > Zombie VR > Build APK**

## Configurações usadas

| Configuração | Valor |
| --- | --- |
| Application ID | `com.brunomouramathias.zombievr` |
| Nível mínimo de API | 26 |
| Nível de API alvo | Automático (maior instalado) |
| Backend de scripts | IL2CPP |
| Arquitetura alvo | ARM64 |
| API gráfica | OpenGL ES 3 / Vulkan (compatível com Cardboard) |

## Instalando no celular

1. Copie o `.apk` para o dispositivo.
2. Permita a instalação de fontes desconhecidas para o gerenciador de arquivos
   usado.
3. Instale e abra o aplicativo.
4. Conceda as permissões solicitadas.
5. Coloque o celular no visor Cardboard e ajuste o perfil do visor se
   necessário.

## Solução de problemas

- **O jogo abre em tela única, sem estéreo**: confirme que o XR Plug-in
  Management está habilitado para Android e que o loader do Cardboard está
  selecionado.
- **Sem rastreamento de cabeça**: verifique se o dispositivo tem giroscópio
  funcionando e se o aplicativo está em orientação paisagem.
- **O build falha em uma máquina nova**: instale os módulos Android SDK, NDK e
  OpenJDK pelo Unity Hub e reabra o projeto.
