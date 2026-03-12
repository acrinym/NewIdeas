param()

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$secretsPath = Join-Path $repoRoot "secrets.json"
$mcpPath = Join-Path $repoRoot "mcp.json"

if (-not (Test-Path $secretsPath))
{
    throw "Missing secrets.json at $secretsPath"
}

$secrets = Get-Content $secretsPath -Raw | ConvertFrom-Json
$context7ApiKey = $null

if ($null -ne $secrets.context7 -and $null -ne $secrets.context7.apiKey -and -not [string]::IsNullOrWhiteSpace($secrets.context7.apiKey))
{
    $context7ApiKey = [string]$secrets.context7.apiKey
}
elseif ($null -ne $secrets.Context7ApiKey -and -not [string]::IsNullOrWhiteSpace($secrets.Context7ApiKey))
{
    $context7ApiKey = [string]$secrets.Context7ApiKey
}

if ([string]::IsNullOrWhiteSpace($context7ApiKey))
{
    throw "Context7 API key was not found in secrets.json. Use either context7.apiKey or Context7ApiKey."
}

$filesystemRoot = $repoRoot.Replace("\", "/")

$config = [ordered]@{
    mcpServers = [ordered]@{
        "desktop-commander" = [ordered]@{
            command = "npx"
            args = @(
                "-y",
                "@wonderwhy-er/desktop-commander@latest"
            )
        }
        "Filesystem" = [ordered]@{
            command = "npx"
            args = @(
                "-y",
                "@modelcontextprotocol/server-filesystem",
                $filesystemRoot
            )
            env = [ordered]@{}
        }
        "Sequential Thinking" = [ordered]@{
            command = "npx"
            args = @(
                "-y",
                "@modelcontextprotocol/server-sequential-thinking"
            )
            env = [ordered]@{}
        }
        "Fetch" = [ordered]@{
            command = "uvx"
            args = @(
                "mcp-server-fetch"
            )
            env = [ordered]@{}
        }
        "everything-search" = [ordered]@{
            command = "uvx"
            args = @(
                "mcp-server-everything-search"
            )
            env = [ordered]@{
                EVERYTHING_SDK_PATH = "C:/Utils/Everything-SDK/dll/Everything64.dll"
            }
        }
        "context7" = [ordered]@{
            command = "npx"
            args = @(
                "-y",
                "@upstash/context7-mcp@latest"
            )
            env = [ordered]@{
                CONTEXT7_API_KEY = $context7ApiKey
            }
        }
    }
}

$json = $config | ConvertTo-Json -Depth 8
Set-Content -Path $mcpPath -Value $json

Write-Host "Wrote local MCP config to $mcpPath"
