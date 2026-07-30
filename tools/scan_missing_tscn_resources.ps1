$root = 'C:\Users\Zinna\Desktop\Project3'
Get-ChildItem -Path $root -Recurse -Filter '*.tscn' | ForEach-Object {
  $p = $_.FullName
  $text = Get-Content $p -Raw -ErrorAction SilentlyContinue
  if($text) {
    $pattern = 'path="res://([^\"]+)"'
    $matches = [regex]::Matches($text, $pattern)
    foreach($m in $matches) {
      $res = $m.Groups[1].Value
      $full = Join-Path $root ($res -replace '/','\\')
      if(-not (Test-Path $full)) {
        Write-Output "$res -> $($p.Substring($root.Length+1))"
      }
    }
  }
}
