# Coinbase OAuth Demo

Esta muestra ilustra cómo ejecutar el flujo de autorización de Coinbase desde una Razor Page. El flujo completo ahora incluye:

1. **Generación de `state`**: Cuando se visita la página sin un código de autorización, la aplicación genera un valor `state` aleatorio, lo guarda en la sesión del usuario y lo agrega a la URL de autorización de Coinbase.
2. **Persistencia con sesión**: El middleware de sesiones almacena el `state` generado de forma segura entre solicitudes para poder validarlo después del redireccionamiento de Coinbase.
3. **Intercambio automático del código**: Una vez que Coinbase redirige de regreso con un `code`, la página valida que el `state` coincida con el guardado y envía una solicitud `POST` a `https://api.coinbase.com/oauth/token` para intercambiarlo por tokens. El token de acceso obtenido se muestra en la interfaz o, en caso de error, se presentan los mensajes correspondientes.

Configura en `appsettings.json` o variables de entorno los valores `Coinbase:ClientId`, `Coinbase:ClientSecret`, `Coinbase:RedirectUri` y, opcionalmente, `Coinbase:Scope` para que la demostración funcione correctamente.
