# Example2D dependencies

Tested with Unity 6000.3.10f1 and DODBT-Light 1.0.0.

Add these entries to the `dependencies` object in your project's `Packages/manifest.json`, keeping existing entries. Install them before importing the sample.

```json
{
  "com.leopotam.ecslite": "https://github.com/Leopotam/ecslite.git#153dfd4043e1471a01a201624dcbebb25436b638",
  "com.leopotam.ecslite.di": "https://github.com/Leopotam/ecslite-di.git#482b14e87c09cd1bf3f8f3dffc5c0099f00a693a",
  "com.leopotam.ecslite.unityeditor": "https://github.com/Leopotam/ecslite-unityeditor.git#f2fc334a935edae2a327535614fa3976d636334f",
  "com.h8man.2d.navmeshplus": "https://github.com/h8man/NavMeshPlus.git#725349b17aad2dff5153a2ea66970028813fbeed",
  "com.unity.ai.navigation": "2.0.11",
  "com.unity.inputsystem": "1.18.0",
  "com.unity.render-pipelines.universal": "17.3.0",
  "com.unity.2d.sprite": "1.0.0",
  "com.unity.2d.tilemap": "1.0.0",
  "com.unity.2d.tilemap.extras": "4.1.0",
  "com.unity.ugui": "2.0.0"
}
```

The sample also uses Unity's built-in Physics 2D, AI, UI, and Tilemap modules.
