# bags-amazon-synchronizer
Synchronizes bags.zoltu.com with Amazon.com.


## Docker

### Build
```
docker build -t bags-amazon-synchronizer .
```

### Run
```
docker run --rm -e AmazonAssociateTag=foo -e AmazonAccessKeyId=bar -e AmazonSecretAccessKey=zip -e SqlServerConnectionString=zap bags-amazon-synchronizer
```
