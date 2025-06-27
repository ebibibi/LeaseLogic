import React, { useState, useCallback, useRef } from 'react';
import { Upload, File, AlertCircle } from 'lucide-react';
import { apiService } from '../services/api';
import { AnalysisState } from '../types';

interface FileUploadProps {
  onAnalysisStart: (analysis: AnalysisState) => void;
  disabled?: boolean;
}

const FileUpload: React.FC<FileUploadProps> = ({ onAnalysisStart, disabled }) => {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);
  const [dragOver, setDragOver] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const validateFile = useCallback((file: File): string | null => {
    const maxSize = 50 * 1024 * 1024; // 50MB
    const allowedTypes = [
      'application/pdf',
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      'application/msword',
      'text/plain'
    ];

    if (file.size > maxSize) {
      return 'ファイルサイズが50MBを超えています。';
    }

    if (!allowedTypes.includes(file.type)) {
      return 'サポートされていないファイル形式です。PDF、Word、テキストファイルのみ対応しています。';
    }

    return null;
  }, []);

  const handleFileSelect = useCallback((file: File) => {
    const validationError = validateFile(file);
    if (validationError) {
      setError(validationError);
      return;
    }

    setSelectedFile(file);
    setError(null);
  }, [validateFile]);

  const handleFileInputChange = useCallback((event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      handleFileSelect(file);
    }
  }, [handleFileSelect]);

  const handleDrop = useCallback((event: React.DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    setDragOver(false);

    const file = event.dataTransfer.files[0];
    if (file) {
      handleFileSelect(file);
    }
  }, [handleFileSelect]);

  const handleDragOver = useCallback((event: React.DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    setDragOver(true);
  }, []);

  const handleDragLeave = useCallback((event: React.DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    setDragOver(false);
  }, []);

  const handleUploadClick = useCallback(() => {
    fileInputRef.current?.click();
  }, []);

  const handleAnalyze = useCallback(async () => {
    if (!selectedFile) return;

    setUploading(true);
    setError(null);

    try {
      // Step 1: Get upload URL
      const uploadUrlResponse = await apiService.generateUploadUrl({
        fileName: selectedFile.name,
        fileSize: selectedFile.size,
        contentType: selectedFile.type,
      });

      // Step 2: Upload file to blob storage
      await apiService.uploadFile(uploadUrlResponse.uploadUrl, selectedFile);

      // Step 3: Start analysis
      const analysisResponse = await apiService.startAnalysis({
        fileId: uploadUrlResponse.fileId,
        fileName: selectedFile.name,
        fileSize: selectedFile.size,
        contentType: selectedFile.type,
        options: {
          language: 'ja',
          detailLevel: 'standard',
        },
      });

      // Notify parent component
      onAnalysisStart({
        analysisId: analysisResponse.analysisId,
        fileName: selectedFile.name,
        status: 'Running',
        progress: {
          currentStep: 'Initializing',
          percentage: 0,
          message: '解析を開始しています...',
        },
      });

      // Reset form
      setSelectedFile(null);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    } catch (error) {
      console.error('Upload/Analysis error:', error);
      setError('アップロードまたは解析の開始に失敗しました。もう一度お試しください。');
    } finally {
      setUploading(false);
    }
  }, [selectedFile, onAnalysisStart]);

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  return (
    <section className="upload-section">
      <h2>契約書アップロード</h2>
      
      <div
        className={`upload-area ${dragOver ? 'drag-over' : ''}`}
        onDrop={handleDrop}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onClick={handleUploadClick}
      >
        <div className="upload-icon">
          <Upload size={48} />
        </div>
        <div className="upload-text">
          <p>ファイルをドラッグ&ドロップするか、クリックしてファイルを選択してください</p>
          <p style={{ fontSize: '0.9rem', color: '#888' }}>
            対応形式: PDF, Word, テキスト (最大50MB)
          </p>
        </div>
        <button 
          type="button" 
          className="upload-button"
          disabled={disabled || uploading}
        >
          ファイルを選択
        </button>
        
        <input
          ref={fileInputRef}
          type="file"
          className="file-input"
          accept=".pdf,.doc,.docx,.txt"
          onChange={handleFileInputChange}
          disabled={disabled || uploading}
        />
      </div>

      {selectedFile && (
        <div className="file-info">
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <File size={20} />
            <div>
              <div style={{ fontWeight: 'bold' }}>{selectedFile.name}</div>
              <div style={{ fontSize: '0.9rem', color: '#666' }}>
                {formatFileSize(selectedFile.size)} • {selectedFile.type}
              </div>
            </div>
          </div>
          
          <button
            type="button"
            className="upload-button"
            onClick={handleAnalyze}
            disabled={disabled || uploading}
            style={{ marginTop: '1rem' }}
          >
            {uploading ? (
              <>
                <span className="loading-spinner"></span>
                アップロード中...
              </>
            ) : (
              '解析開始'
            )}
          </button>
        </div>
      )}

      {error && (
        <div className="error-message">
          <AlertCircle size={20} style={{ marginRight: '0.5rem' }} />
          {error}
        </div>
      )}
    </section>
  );
};

export default FileUpload;