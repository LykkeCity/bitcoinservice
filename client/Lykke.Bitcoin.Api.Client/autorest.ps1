# Run this file if you use PowerShell directly
autorest -Input http://localhost:8080/swagger/v1/swagger.json -CodeGenerator CSharp -OutputDirectory ./AutoGenerated -Namespace Lykke.Bitcoin.Api.Client.AutoGenerated