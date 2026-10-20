# Upgrading from DODBT 0.4.0

Version 1.0.0 splits DODBT into two independent packages. **DODBT-Light is the successor to the C# version. DODBT-Dots is a separate Unity Entities implementation.**

This is a breaking upgrade. There is no automatic migration of existing graphs or compiled assets.

1. Back up your project, custom leaves, graphs, and compiled assets before changing packages.
2. Remove `com.vadimburym.dodbt` from Package Manager, or remove the old plugin folder if it was installed in `Assets`. Keep your own code and assets in a separate folder.
3. Install `com.vadimburym.dodbt.light`. Do not keep an old `Assets` copy alongside the UPM package.
4. Change your imports from `VadimBurym.DodBehaviourTree` to `VadimBurym.DodBehaviourTree.Light`. Update custom assembly references to `VadimBurym.DodBehaviourTree.Light` and, for editor code, `VadimBurym.DodBehaviourTree.Light.Editor`.
5. Review your context and leaf state types. Select both types in the new graph editor. Existing `ILeaf<TContext, TLeafState>` implementations can be used to discover these types.
6. Recreate your graphs in the Light editor and compile them again. Package script GUIDs and assembly identities changed; old graph and compiled-asset references are not automatically remapped. Preserve the old project as a reference until migration is complete.
7. Assign the new compiled assets to your integration and check ticking, cancellation, state cleanup, and debugging in your own project.

To adopt DODBT-Dots, follow its [integration guide](VadimBurym-DODBT-Dots/DOCUMENTATION-en.md). Managed Light contexts and leaf state are not interchangeable with Dots contexts, blobs, or entity buffers.
