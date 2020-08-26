# UWP File Explorer

This solution creates a File Explorer using UWP.  It was created to explore how to create a file explorer that performs adequately given the incredibly slow speeds of the UWP filesystem API.

# Requirements

The following are requirements of the solution:

* Must work under UWP container constraints (no `BroadFileSystemAccess`)
* Read and display the filtered contents of a root folder
* Allow navigation down and up within the root folder's folder hierarchy
* Display basic properties of selected files and folders
* Perform adequately with large folders without blocking the UI
* Monitor and update the UI when the contents of the currently displayed folder changes

# Notes

* `...FromApp` PInvoke functions were trialed for best performance.  Unfortunately this function crashed the app on certain folders and could not be used in the final solution.
* The best hope for an API that improves on the ghastly performance of the UWP filesystem API is the `ProjectReunion` improvements.  I've added our needs to a suitable [discussion on GitHub](https://github.com/microsoft/ProjectReunion/issues/8#issuecomment-679910789).
* Kudos provided in code where appropriate.