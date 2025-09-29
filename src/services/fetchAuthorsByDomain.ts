// Author API Client - TypeScript Example
// This file demonstrates how to authenticate and fetch data from the Author API

import { AuthenticationProvider, AuthenticationResult } from '@azure/msal-node';

/**
 * Author response structure as defined by the API
 */
export interface AuthorApiResponse {
  id: string;
  AuthorName: string;
  LanguageName: string;
  RegionName: string;
  EmailAddress: string;
  WelcomeText: string;
  AboutText: string;
  HeadShotURL: string;
  CopyrightText: string;
  TopLevelDomain: string;
  SecondLevelDomain: string;
  Articles: Article[];
  Books: Book[];
  Socials: Social[];
}

export interface Article {
  Title: string;
  Date: string;
  Publication: string;
  Url: string;
}

export interface Book {
  Title: string;
  Description: string;
  Url: string;
  Cover: string;
}

export interface Social {
  Name: string;
  Url: string;
}

/**
 * Configuration for the Author API client
 */
export interface AuthorApiConfig {
  /** The base URL of the API (e.g., https://your-function-app.azurewebsites.net) */
  apiBaseUrl: string;
  /** Microsoft Entra ID (Azure AD) tenant ID */
  tenantId: string;
  /** Client ID of your application registered in Azure AD */
  clientId: string;
  /** Client secret (only for server-side applications) */
  clientSecret?: string;
  /** The API scope required for authentication */
  apiScope: string; // e.g., "api://your-api-client-id/Author.Read"
}

/**
 * Service class for interacting with the Author API
 */
export class AuthorApiService {
  private config: AuthorApiConfig;

  constructor(config: AuthorApiConfig) {
    this.config = config;
  }

  /**
   * Authenticates with Microsoft Entra ID and returns an access token
   * This example uses the Client Credentials flow for server-to-server authentication
   */
  private async getAccessToken(): Promise<string> {
    const { PublicClientApplication, ConfidentialClientApplication } = await import('@azure/msal-node');
    
    if (!this.config.clientSecret) {
      throw new Error('Client secret is required for server-to-server authentication');
    }

    const clientConfig = {
      auth: {
        clientId: this.config.clientId,
        clientSecret: this.config.clientSecret,
        authority: `https://login.microsoftonline.com/${this.config.tenantId}`,
      },
    };

    const cca = new ConfidentialClientApplication(clientConfig);

    const clientCredentialRequest = {
      scopes: [this.config.apiScope],
    };

    try {
      const response: AuthenticationResult = await cca.acquireTokenByClientCredential(clientCredentialRequest);
      
      if (!response || !response.accessToken) {
        throw new Error('Failed to acquire access token');
      }

      return response.accessToken;
    } catch (error) {
      console.error('Authentication failed:', error);
      throw new Error(`Authentication failed: ${error}`);
    }
  }

  /**
   * Fetches all authors for a given domain
   * @param secondLevelDomain The second-level domain (e.g., "example" from "example.com")
   * @param topLevelDomain The top-level domain (e.g., "com" from "example.com")
   * @returns Promise<AuthorApiResponse[]> Array of authors for the domain
   */
  async fetchAuthorsByDomain(
    secondLevelDomain: string, 
    topLevelDomain: string
  ): Promise<AuthorApiResponse[]> {
    try {
      // Get access token
      const accessToken = await this.getAccessToken();

      // Build API endpoint URL
      const apiUrl = `${this.config.apiBaseUrl}/api/authors/${encodeURIComponent(secondLevelDomain)}/${encodeURIComponent(topLevelDomain)}`;

      // Make API request
      const response = await fetch(apiUrl, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${accessToken}`,
          'Content-Type': 'application/json',
        },
      });

      // Handle response
      if (!response.ok) {
        if (response.status === 401) {
          throw new Error('Unauthorized: Invalid or expired access token');
        }
        if (response.status === 403) {
          throw new Error('Forbidden: Insufficient permissions');
        }
        if (response.status === 404) {
          throw new Error('Not Found: Domain not found');
        }
        if (response.status === 500) {
          throw new Error('Internal Server Error: Unexpected error occurred');
        }
        
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      const authors: AuthorApiResponse[] = await response.json();
      return authors;

    } catch (error) {
      console.error('Error fetching authors by domain:', error);
      throw error;
    }
  }
}

/**
 * Example usage of the Author API service
 */
export async function exampleUsage() {
  // Configure the API client
  const config: AuthorApiConfig = {
    apiBaseUrl: 'https://your-function-app.azurewebsites.net',
    tenantId: 'your-tenant-id',
    clientId: 'your-client-id',
    clientSecret: 'your-client-secret', // Only for server-side apps
    apiScope: 'api://your-api-client-id/Author.Read',
  };

  // Create service instance
  const authorApi = new AuthorApiService(config);

  try {
    // Fetch authors for example.com
    const authors = await authorApi.fetchAuthorsByDomain('example', 'com');
    
    console.log(`Found ${authors.length} authors for example.com:`);
    authors.forEach(author => {
      console.log(`- ${author.AuthorName} (${author.EmailAddress})`);
      console.log(`  Language: ${author.LanguageName}, Region: ${author.RegionName}`);
      console.log(`  Books: ${author.Books.length}, Articles: ${author.Articles.length}`);
    });
    
  } catch (error) {
    console.error('Failed to fetch authors:', error);
  }
}

// For browser-based authentication (Authorization Code flow), 
// you would use @azure/msal-browser instead and implement the 
// interactive sign-in flow. This example focuses on server-to-server scenarios.