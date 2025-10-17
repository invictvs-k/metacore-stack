import fetch, { RequestInit, Response } from 'node-fetch';

export async function makeRequest(url: string, init?: RequestInit): Promise<Response> {
  return fetch(url, init);
}

console.log('[http.request] placeholder module ready');
