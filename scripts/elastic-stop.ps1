<#
	Stop Elastic
#>
$CNAME="belastic"
$NETWORK="elasticnetwork"

docker network rm $NETWORK 2>&1 | out-null
docker stop "${CNAME}" 2>&1 | out-null
docker rm "${CNAME}" 2>&1 | out-null