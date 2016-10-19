To port the library to .net, it is still required to
1- Port LightNode to .Net core
2- Port the OWIN code

The command used to generate entities from database is as follows:
Scaffold-DbContext "Server=.\SQLExpress;Database=AssetLightning;Trusted_Connection=True;" Microsoft.EntityFrameworkCore.SqlServer -OutputDir DbModels
A dummy main main is added to cover the command requirement to have an entry point

The following original strings from project.json , may also help

// TODO: What is the exact difference between netstandard1.6 and netcoreapp1.0
// necoreapp supports entity framework tools not the netstandard
  "frameworks": {
    "netstandard1.6": {
      "imports": "dnxcore50"
    }
  }


  // For entity framework tools to work
  "frameworks": {
    "netcoreapp1.0": {
        "imports": [
          "dnxcore50"
        ]
      }
    },
