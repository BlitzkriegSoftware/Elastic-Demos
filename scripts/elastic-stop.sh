#!/bin/bash
export CNAME=belastic
docker stop "${CNAME}"
docker rm "${CNAME}"