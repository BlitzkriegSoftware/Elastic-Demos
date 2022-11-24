<#
	Stop Elastic
#>
$CNAME="belastic"
docker stop "${CNAME}" 2>&1 | out-null
docker rm "${CNAME}" 2>&1 | out-null