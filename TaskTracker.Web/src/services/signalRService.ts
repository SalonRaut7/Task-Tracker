import * as signalR from "@microsoft/signalr";
import { ACCESS_TOKEN_KEY } from "./apiClient";

const rawBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "";
const HUB_URL = `${rawBaseUrl.replace(/\/+$/, "")}/hubs/notifications`;

let connection: signalR.HubConnection | null = null;

export function getConnection(): signalR.HubConnection | null {
  return connection;
}

export function buildConnection(): signalR.HubConnection {
  if (connection) {
    return connection;
  }

  connection = new signalR.HubConnectionBuilder()
    .withUrl(HUB_URL, {
      accessTokenFactory: () => localStorage.getItem(ACCESS_TOKEN_KEY) ?? "",
    })
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (retryContext) => {
        // Exponential backoff: 0s, 2s, 5s, 10s, 30s, then 30s forever
        const delays = [0, 2000, 5000, 10000, 30000];
        return delays[Math.min(retryContext.previousRetryCount, delays.length - 1)];
      },
    })
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  return connection;
}

export async function startConnection(): Promise<void> {
  const conn = buildConnection();

  if (conn.state === signalR.HubConnectionState.Connected) {
    return;
  }

  if (conn.state === signalR.HubConnectionState.Connecting) {
    return;
  }

  try {
    await conn.start();
    console.log("[SignalR] Connected to NotificationHub");
  } catch (err) {
    console.error("[SignalR] Connection failed:", err);
    // Will auto-reconnect via withAutomaticReconnect
  }
}

export async function stopConnection(): Promise<void> {
  if (connection) {
    try {
      await connection.stop();
      console.log("[SignalR] Disconnected");
    } catch (err) {
      console.error("[SignalR] Stop failed:", err);
    }
    connection = null;
  }
}
