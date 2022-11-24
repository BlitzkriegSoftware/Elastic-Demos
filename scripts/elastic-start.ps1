<#
	Start Elastic
#>
$CNAME="belastic"
$IMAGE="elasticsearch:8.5.1"

docker stop "${CNAME}" 2>&1 | out-null
docker rm "${CNAME}" 2>&1 | out-null
docker pull $IMAGE
docker run -d --name "${CNAME}" -p 9200:9200 -p 9300:9300 -e "ELASTIC_CLIENT_APIVERSIONING=true" -e "discovery.type=single-node" $IMAGE