# WindowsRuntimeResXSupport
This library provides a [`ResourceManager`](https://msdn.microsoft.com/en-us/library/system.resources.resourcemanager(v=vs.110).aspx) which both Windows RT and non-Windows-RT projects can use to retrieve resources located in a Shared C# project.

## Why?
I created a Xamarin app that also needed to support UWP. While the WindowsPhone project could only load ResX files, the UWP project could only load the new ResW files. To get around this issue, I needed to apply [this workaround](https://blogs.msdn.microsoft.com/philliphoff/2014/11/19/missingmanifestresourceexception-when-using-portable-class-libraries-within-winrt/) and inject a new `ResourceManager` at runtime which could handle both.

## Usage
Refer to the inline documentation.
