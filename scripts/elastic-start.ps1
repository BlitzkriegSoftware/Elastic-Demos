<#
	Start Elastic
#>
$CNAME="belastic"
$IMAGE="elasticsearch:8.5.1"
$NETWORK="elasticnetwork"

docker stop "${CNAME}" 2>&1 | out-null
docker rm "${CNAME}" 2>&1 | out-null
docker network rm $NETWORK 2>&1 | out-null
docker pull $IMAGE

docker network create $NETWORK

docker run -d `
	--net "${NETWORK}" `
	--name "${CNAME}" `
	-p 9200:9200 `
	-p 9300:9300 `
	-e "ELASTIC_CLIENT_APIVERSIONING=true" `
	-e "discovery.type=single-node" `
	-e "ES_JAVA_OPTS=-Xms1g -Xmx1g" `
	-e "xpack.security.enabled=false" `
	$IMAGE

curl http://localhost:9200