# Only
This library enables a WPF app to run as a single instance.

[![Downloads](https://img.shields.io/nuget/dt/Only.svg)](https://www.nuget.org/packages/Only)
[![NuGet](https://img.shields.io/nuget/v/Only.svg)](https://www.nuget.org/packages/Only)
[![MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/vborovikov/only/blob/master/LICENSE)

# Usage

Get the `Only` package from NuGet and update your `App.xaml` file as follows:
```xml
<only:InstanceAwareApp x:Class="YourApp.App"
    xmlns:only="clr-namespace:Only;assembly=Only">
...
</only:InstanceAwareApp>
```
Then modify `App.xaml.cs` to call `Only.InstanceAwareApp.RunSingle()` method:
```cs
public partial class App
{
    public new int Run()
    {
        return RunSingle();
    }
}
```

That's it!