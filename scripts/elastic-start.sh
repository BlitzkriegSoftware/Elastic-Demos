#!/bin/bash
export CNAME=belastic
docker pull docker.elastic.co/elasticsearch/elasticsearch:7.13.4
docker run -d --name "${CNAME}" -p 9200:9200 -p 9300:9300 -e "discovery.type=single-node" docker.elastic.co/elasticsearch/elasticsearch:7.13.4