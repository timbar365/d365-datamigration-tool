# Dynamics 365 Data Migration Tool

This repository contains a console utility for exporting data from and importing data into Dynamics 365 using the Web API. It supports exporting entity data to a JSON payload based on an XML configuration and re-importing that payload with optional field and owner mappings.

## Prerequisites
- .NET 8 SDK for building and running the tool.
- A Dynamics 365 environment with either user credentials or an app registration for OAuth/client credential authentication.

## Building the tool
From the repository root:
```bash
dotnet restore
dotnet build Tools.DataMigration.sln -c Release
```
The compiled console app is placed under `Source/bin/Release/net8.0/`. Run commands from that folder so the bundled `log4net.config` file can be discovered.

## Command-line parameters
Run the tool from the `Source` output folder. Required parameters are marked accordingly. Authentication depends on the chosen `--authtype`:
- `Office365` (default): supply `--user` and `--password`.
- `ClientId`: supply `--tenantid`, `--clientid`, and `--clientsecret` for client credentials.
- `OAuth`: launches interactive browser authentication; provide `--clientid` and optionally `--tenantid` for the authority.

| Parameter | Alias | Required | Description |
| --- | --- | --- | --- |
| `--url` | – | Yes | Dynamics 365 environment URL. |
| `--mode` | `-m` | Yes | `export` or `import` to control the operation. |
| `--file` | `-f` | Yes | Path to the data file to read (`import`) or write (`export`). |
| `--authtype` | `-a` | No | Authentication type (`Office365`, `ClientId`, or `OAuth`). Defaults to `Office365`. |
| `--user` | `-u` | No | Username for user/password auth. |
| `--password` | `-p` | No | Password for user/password auth. |
| `--tenantid` | `-t` | No | Tenant ID for application auth. |
| `--clientid` | `-c` | No | Client ID / App ID for application auth. |
| `--clientsecret` | `-s` | No | Client secret for application auth. |
| `--expconf` | `-e` | No | Path to the XML export configuration file. Required for export mode. |
| `--ownermap` | `-o` | No | Path to owner mapping JSON used during import. |
| `--fieldimportmap` | – | No | Path to entity/field mapping JSON used during import. |
| `--verbose` | `-v` | No | Enable verbose logging. |
| `--returnError` | – | No | Return exit code `-1` if any import errors occur. Defaults to `false`. |
| `--timeout` | – | No | Web request timeout in milliseconds. Defaults to `100000`. |
| `--parallelLimit` | – | No | Parallel import thread limit. Defaults to `5`. |

## Export configuration schema (`.xml`)
Exports are driven by an XML file matching the `entities` schema in `Source/Types/ConfigurationSchema.cs`. The root contains one or more `<entity>` entries.

### Example
```xml
<entities>
  <entity logicalname="account" exportowner="true" exportcreatedon="false" disableplugins="false" deactivateAllRecords="false" ensureWithAllFields="false" ensureWithFields="accountnumber, telephone1" fieldsToIgnore="importsequencenumber">
    <fetchfilter>
      <... fetchxml ...>
    </fetchfilter>
  </entity>
</entities>
```

### `<entity>` attributes
- `logicalname` (**required**): Entity logical name.
- `exportowner`: Include owner lookup data. Default `false` removes `_ownerid` fields.
- `exportcreatedon`: Include `createdon`. Default `false` removes creation audit fields.
- `disableplugins`: Skip plugins during import for this entity.
- `deactivateAllRecords`: Deactivate existing records before import.
- `fieldsToIgnore`: Comma-separated list of attributes to drop before import.
- `ensureWithAllFields`: When `true`, all exported fields are used when ensuring record availability.
- `ensureWithFields`: Comma-separated subset of fields to use when ensuring existing records (alternative to `ensureWithAllFields`).
- `skipImport`: If `true`, export the data but skip importing it later.
- `alternateKeyField`: Attribute to use as an alternate key when mapping records.

### `<entity>` children
- `<fetchfilter>`: FetchXML filter applied during export. The inner XML should contain FetchXML element; the tool injects this into the query used for paging exports.

## Export output (`.json`)
Running `export` produces a JSON file representing an `ImportJobDefinition` (see `Source/Types/ImportDefinitions.cs`). Each exported entity becomes an `ImportEntityDefinition` with:
- `EntityLogicalName`, `EntityCollectionName`, `PrimaryAttributeId`, `PrimaryAttributeName`, and optional `AlternateKeyAttribute` inferred from metadata.
- Configuration flags copied from the XML (`DisablePlugins`, `DeactivateAllRecords`, `EnsureWithAllFields`, `EnsureWithFields`, `SkipImport`, `FieldsToIgnore`).
- `Entities`: Array of record payloads with attribute values ready for import.

### Preserve and ignore behavior
- When `exportcreatedon` is `false`, creation audit fields are removed during export; `exportowner=false` removes owner fields. Other system-managed attributes such as `_owning*`, `modifiedon`, or `importsequencenumber` are dropped automatically.
- `fieldsToIgnore` values remain in the export but are skipped when importing.

## Import behavior
When running `import`, the tool:
1. Loads the JSON export file.
2. Applies optional field mappings (`--fieldimportmap`) to rename entities/attributes and adjust lookups.
3. Applies optional owner mappings (`--ownermap`) to remap owners.
4. Ensures referenced records exist (using `EnsureWithAllFields` or `EnsureWithFields` guidance), deactivates records if requested, and performs the final import respecting `FieldsToIgnore` and alternate keys.

## Field mapping schema (`--fieldimportmap`)
A JSON file matching `EntityMapping` (`Source/Types/EntityMapping.cs`) allows renaming entities and attributes during import:
```json
{
  "EntityMappings": [
    {
      "Source": "old_logicalname",
      "Target": "new_logicalname",
      "Fields": [
        { "Source": "old_attribute", "Target": "new_attribute" }
      ]
    }
  ]
}
```
Mappings also update lookup logical names and any configured `EnsureWithFields`, `FieldsToIgnore`, or `AlternateKeyAttribute` values.

## Owner mapping schema (`--ownermap`)
Provide a JSON file matching `OwnerMapping` (`Source/Types/OwnerMapping.cs`) to translate owners:
```json
{
  "OwnerMapping": [
    { "SourceId": "<guid-from-export>", "TargetId": "<guid-to-use>" }
  ]
}
```

## Running examples
- Export accounts with a configuration file:
  ```bash
  dotnet DataMigration.dll --mode export --url https://org.crm.dynamics.com --expconf ./config/entities.xml --file ./output/accounts.json
  ```
- Import the exported data with owner and field mappings:
  ```bash
  dotnet DataMigration.dll --mode import --url https://org.crm.dynamics.com --file ./output/accounts.json --ownermap ./config/owners.json --fieldimportmap ./config/fieldmap.json
  ```
