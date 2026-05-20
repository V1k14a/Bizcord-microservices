# API Gateway

This is a simple API gateway based on Ocelot. It is used to route requests from client applications to the appropriate microservices.

---

## Running the API Gateway

The API gateway can be run using the `docker-compose.yml` file. To run the API gateway, use the following command:

```bash
docker-compose -f docker-compose.yml up
```

> [!IMPORTANT]
> By default, the API gateway will run on port `8080` on the host machine.
