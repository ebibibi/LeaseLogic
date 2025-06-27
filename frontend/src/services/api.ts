import axios from 'axios';
import { 
  UploadUrlRequest, 
  UploadUrlResponse, 
  AnalysisRequest, 
  AnalysisStartResponse,
  AnalysisStatusResponse,
  AnalysisResult 
} from '../types';

// Get API base URL from environment or use development default
const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'http://localhost:7071';

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
});

// Add request interceptor for logging
api.interceptors.request.use(
  (config) => {
    console.log(`API Request: ${config.method?.toUpperCase()} ${config.url}`, config.data);
    return config;
  },
  (error) => {
    console.error('API Request Error:', error);
    return Promise.reject(error);
  }
);

// Add response interceptor for logging
api.interceptors.response.use(
  (response) => {
    console.log(`API Response: ${response.status}`, response.data);
    return response;
  },
  (error) => {
    console.error('API Response Error:', error);
    return Promise.reject(error);
  }
);

export const apiService = {
  // Generate upload URL
  async generateUploadUrl(request: UploadUrlRequest): Promise<UploadUrlResponse> {
    const response = await api.post<UploadUrlResponse>('/api/upload-url', request);
    return response.data;
  },

  // Upload file to blob storage
  async uploadFile(uploadUrl: string, file: File): Promise<void> {
    await axios.put(uploadUrl, file, {
      headers: {
        'x-ms-blob-type': 'BlockBlob',
        'Content-Type': file.type,
      },
      timeout: 300000, // 5 minutes for large files
    });
  },

  // Start analysis
  async startAnalysis(request: AnalysisRequest): Promise<AnalysisStartResponse> {
    const response = await api.post<AnalysisStartResponse>('/api/analyze', request);
    return response.data;
  },

  // Get analysis status
  async getAnalysisStatus(analysisId: string): Promise<AnalysisStatusResponse> {
    const response = await api.get<AnalysisStatusResponse>(`/api/status/${analysisId}`);
    return response.data;
  },

  // Get analysis result
  async getAnalysisResult(analysisId: string): Promise<AnalysisResult> {
    const response = await api.get<AnalysisResult>(`/api/result/${analysisId}`);
    return response.data;
  },

  // Delete file
  async deleteFile(fileId: string): Promise<void> {
    await api.delete(`/api/file/${fileId}`);
  },
};

export default apiService;