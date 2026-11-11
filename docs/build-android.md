# Building the Android APK

## Requirements

- Unity 6000.5.9f1.
- Android Build Support installed through Unity Hub, including:
  - Android SDK and NDK
  - OpenJDK
- A device with Android 8.0 (API 26) or newer for testing.

## Steps

1. Open the project in Unity 6000.5.9f1.
2. Open `Assets/Scenes/Zombie.unity`.
3. Go to **File > Build Settings**.
4. Select **Android** and click **Switch Platform**.
5. Make sure the scene list contains only `Assets/Scenes/Zombie.unity`.
6. Click **Build** and choose the output path.

The project also includes a small editor command that builds the same scene to
`Builds/ZombieVR.apk`:

- **Tools > Zombie VR > Build APK**

## Settings used

| Setting | Value |
| --- | --- |
| Application ID | `com.brunomouramathias.zombievr` |
| Minimum API level | 26 |
| Target API level | Automatic (highest installed) |
| Scripting backend | IL2CPP |
| Target architecture | ARM64 |
| Graphics API | OpenGL ES 3 / Vulkan (Cardboard compatible) |

## Installing on the phone

1. Copy the `.apk` to the device.
2. Allow installation from unknown sources for the file manager used.
3. Install and open the app.
4. Grant any requested permissions.
5. Place the phone in the Cardboard viewer and adjust the viewer profile if
   needed.

## Troubleshooting

- **The game opens in a single flat view**: confirm that XR Plug-in Management
  is enabled for Android and that the Cardboard loader is selected.
- **No head tracking**: check that the device has a working gyroscope and that
  the app is running in landscape orientation.
- **Build fails on a fresh machine**: install the Android SDK, NDK and OpenJDK
  modules through Unity Hub, then reopen the project.
