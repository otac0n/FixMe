<# HACK: Ignore inconclusive result. #>
test.exe | %{ $_.Result -ne "Inconclusive" }
