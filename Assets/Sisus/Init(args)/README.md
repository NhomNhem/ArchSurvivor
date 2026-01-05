# README

## Init(args) in a Nutshell
Init(args) brings Inversion of Control to Unity with a seamlessly integrated toolset
for passing arguments to Objects during their initialization in a type safe manner.
Init(args) also makes it easy to work with interfaces, empowering you to write better, decoupled and easily unit testable code.

### Main Features
* Add Component with arguments.
* Instantiate with arguments.
* Create instance with arguments.
* Create new GameObject with arguments.
* Service framework (a powerful alternative to Singletons).
* Attach plain old class objects to GameObjects using Wrappers.
* Auto-init components when they are added in the editor.
* Assign to read-only fields and properties.
* Type safety thanks to use of generics.
* Reflection free dependency injection.

## Installation

### Importing Init(args) To Your Project

Init(args) can be installed using the Package Manager window inside Unity. You can do so by following these steps:

Open the Package Manager window using the menu item Window > Package Manager.
1. In the dropdown at the top of the list view select My Assets.
2. Find Init(args) in the list view and select it.
3. Click the Download button and wait until the download has finished. If the Download button is greyed out you already have the latest version and can skip to the next step.
4. Click the Import button and wait until the loading bar disappears and the Import Unity Package dialog opens.
5. Click Import and wait until the loading bar disappears. You should not change what items are ticked on the window, unless you know what you're doing, because that could mess up the installation.

Init(args) should now be installed and ready to be used!

### Assembly Definition Best Practices

In order to reference Init(args) classes in your code, you will need to add the appropriate Assembly Definition References:

1. Select the Assembly Definition asset for your code. If you don't have one yet, you can create one at the root of your scripts folder using the context menu item ```Create > Assembly Definition```.
2. In the Assembly Definition References list add references to ```InitArgs``` and ```InitArgs.Interfaces```.

In addition to these two assemblies Init(args) also has the ```InitArgs.Services``` assembly, which contains only the ServiceAttribute.
It is recommended to have a separate folder containing only your service classes and a second Assembly Definition asset at the root of the folder,
and to only add a reference to ```InitArgs.Services``` in that Assembly Definition asset.
This helps optimize the start up times for your application by minimizing the number of types that need to be examined using reflection when initializing your services.

### Scripting Define Symbols

If you don't plan on using the ServiceAttribute to register services in your project you can turn off the service injection feature completely
by adding ```INITY_DISABLE_SERVICES``` to your Scripting Define Symbols list in Project Settings.

## Quick Start Guide

Derive your class from one of the generic MonoBehaviour<T...> base classes instead of MonoBehaviour, and you can receive upto five arguments in your Init function.
```csharp
public class Player : MonoBehaviour<IInputManager>
{
    private IInputManager inputManager;

    protected override void Init(IInputManager inputManager)
    {
        this.inputManager = inputManager;
    }
}
```

Then you can add your component to a GameObject with arguments like this:
```csharp
Player player = gameObject.AddComponent<Player, IInputManager>(inputManager);
```

And you can clone the component from a prefab with arguments like this:
```csharp
Player player = playerPrefab.Instantiate(inputManager);
```

In cases where you can't use a base class you can also implement an IInitializable<T...> interface and manually handle receiving the arguments using InitArgs.TryGet.

### Links
 - [Store Page](https://u3d.as/2Eym) - Please consider reviewing Init(args) here <3
 - [Online Documentation](https://docs.sisus.co/init-args) - The full online documentation
 - [Scripting Reference](https://docs.sisus.co/init-args-reference) - List of all Init(args) classes and their members and information relevant to their use.