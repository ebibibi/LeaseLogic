// API Types
export interface UploadUrlRequest {
  fileName: string;
  fileSize: number;
  contentType: string;
}

export interface UploadUrlResponse {
  fileId: string;
  uploadUrl: string;
  expiresAt: string;
  maxFileSize: number;
}

export interface AnalysisRequest {
  fileId: string;
  fileName: string;
  fileSize: number;
  contentType: string;
  options?: {
    language?: string;
    detailLevel?: string;
    notificationUrl?: string;
  };
}

export interface AnalysisStartResponse {
  analysisId: string;
  status: string;
  statusUrl: string;
  resultUrl: string;
  estimatedDuration: string;
  createdTime: string;
}

export interface AnalysisStatusResponse {
  analysisId: string;
  status: 'Running' | 'Completed' | 'Failed' | 'Terminated';
  fileInfo: {
    fileName: string;
    fileSize: number;
    uploadedAt: string;
  };
  progress: {
    currentStep: string;
    percentage: number;
    message: string;
    estimatedRemaining: string;
  };
  result?: AnalysisResult;
  createdTime: string;
  lastUpdatedTime: string;
}

export interface AnalysisResult {
  analysisId: string;
  fileInfo: {
    fileName: string;
    fileSize: number;
  };
  analysisResult: {
    isLease: boolean;
    confidence: number;
    leaseType: 'OperatingLease' | 'FinanceLease' | 'ServiceContract' | 'NotApplicable';
    summary: {
      contractType: string;
      primaryAsset: string;
      contractPeriod: string;
      monthlyPayment: string;
    };
    leaseAnalysis: {
      identifiedAsset: {
        hasIdentifiedAsset: boolean;
        assetDescription: string;
        assetSpecificity: string;
        citations: string[];
      };
      rightToControl: {
        hasRightToControl: boolean;
        controlIndicators: string[];
        citations: string[];
      };
      substantiveSubstitutionRights: {
        hasSubstitutionRights: boolean;
        analysis: string;
        citations: string[];
      };
    };
    keyFindings: string[];
    riskFactors: string[];
    recommendations: string[];
    complianceRequirements: string[];
  };
  documentSummary: string;
  processingTime: string;
  completedAt: string;
}

// Component State Types
export interface AnalysisState {
  analysisId: string;
  fileName: string;
  status: 'Running' | 'Completed' | 'Failed' | 'Terminated';
  progress?: {
    currentStep: string;
    percentage: number;
    message: string;
  };
  result?: AnalysisResult;
  error?: string;
}

export interface AnalysisHistoryItem {
  analysisId: string;
  fileName: string;
  timestamp: Date;
  status: string;
  isLease?: boolean;
  confidence?: number;
}