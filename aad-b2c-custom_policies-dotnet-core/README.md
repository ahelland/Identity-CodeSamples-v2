## aad-b2c-custom_policies-dotnet-core-linux
### Build Status:  
[![Build status](https://batprojects.visualstudio.com/IdentitySamples/_apis/build/status/aad-b2c-custom_policies-dotnet-core-linux)](https://batprojects.visualstudio.com/IdentitySamples/_build/latest?definitionId=20)

A ready-made Docker image can be found at: [https://hub.docker.com/r/ahelland/aad-b2c-custom_policies-dotnet-core-linux](https://hub.docker.com/r/ahelland/aad-b2c-custom_policies-dotnet-core-linux)

This is a web app that implements multiple identity providers through custom policies in Azure AD B2C.

There is also a sort of "AuthZ Light" implementation where a role claim is emitted through an Azure Function that is called into in the user journey.

The custom policies must be uploaded separately in the Azure AD B2C section of the Azure Portal. (Tenant-specific settings must be set first; the included files are generic and will not work out of the box.)

Azure Functions should be implemented in the Azure Portal as well, and the URL to the function must be used in the Custom Policies.