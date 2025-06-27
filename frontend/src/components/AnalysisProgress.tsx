import React, { useEffect, useCallback } from 'react';
import { Clock, CheckCircle, XCircle, AlertTriangle } from 'lucide-react';
import { apiService } from '../services/api';
import { AnalysisState } from '../types';

interface AnalysisProgressProps {
  analysis: AnalysisState;
  onUpdate: (analysis: AnalysisState) => void;
  onComplete: (analysis: AnalysisState) => void;
}

const AnalysisProgress: React.FC<AnalysisProgressProps> = ({ 
  analysis, 
  onUpdate, 
  onComplete 
}) => {
  const pollStatus = useCallback(async () => {
    try {
      const statusResponse = await apiService.getAnalysisStatus(analysis.analysisId);
      
      const updatedAnalysis: AnalysisState = {
        analysisId: analysis.analysisId,
        fileName: analysis.fileName,
        status: statusResponse.status,
        progress: {
          currentStep: statusResponse.progress.currentStep,
          percentage: statusResponse.progress.percentage,
          message: statusResponse.progress.message,
        },
      };

      if (statusResponse.status === 'Completed') {
        // Get the full result
        try {
          const result = await apiService.getAnalysisResult(analysis.analysisId);
          updatedAnalysis.result = result;
          onComplete(updatedAnalysis);
        } catch (error) {
          console.error('Failed to get analysis result:', error);
          updatedAnalysis.status = 'Failed';
          updatedAnalysis.error = '結果の取得に失敗しました。';
          onUpdate(updatedAnalysis);
        }
      } else if (statusResponse.status === 'Failed') {
        updatedAnalysis.error = '解析処理中にエラーが発生しました。';
        onUpdate(updatedAnalysis);
      } else {
        onUpdate(updatedAnalysis);
      }
    } catch (error) {
      console.error('Failed to poll status:', error);
      onUpdate({
        ...analysis,
        status: 'Failed',
        error: 'ステータスの確認に失敗しました。',
      });
    }
  }, [analysis.analysisId, analysis.fileName, onUpdate, onComplete]);

  useEffect(() => {
    if (analysis.status === 'Running') {
      const interval = setInterval(pollStatus, 3000); // Poll every 3 seconds
      return () => clearInterval(interval);
    }
  }, [analysis.status, pollStatus]);

  const getStatusIcon = () => {
    switch (analysis.status) {
      case 'Running':
        return <Clock size={20} className="loading-spinner" />;
      case 'Completed':
        return <CheckCircle size={20} style={{ color: '#28a745' }} />;
      case 'Failed':
        return <XCircle size={20} style={{ color: '#dc3545' }} />;
      case 'Terminated':
        return <AlertTriangle size={20} style={{ color: '#ffc107' }} />;
      default:
        return <Clock size={20} />;
    }
  };

  const getStatusText = () => {
    switch (analysis.status) {
      case 'Running':
        return '解析実行中';
      case 'Completed':
        return '解析完了';
      case 'Failed':
        return '解析失敗';
      case 'Terminated':
        return '解析中断';
      default:
        return '不明';
    }
  };

  const getStatusBadgeClass = () => {
    switch (analysis.status) {
      case 'Running':
        return 'status-running';
      case 'Completed':
        return 'status-completed';
      case 'Failed':
      case 'Terminated':
        return 'status-failed';
      default:
        return 'status-running';
    }
  };

  const getCurrentStepText = () => {
    if (!analysis.progress) return '';
    
    const stepMap: { [key: string]: string } = {
      'Initializing': '初期化中',
      'DocumentParsing': '文書解析中',
      'ContentStructuring': '内容構造化中',
      'AIAnalysis': 'AI解析中',
      'ReportGeneration': 'レポート生成中',
      'Completed': '完了'
    };

    return stepMap[analysis.progress.currentStep] || analysis.progress.currentStep;
  };

  return (
    <section className="progress-section">
      <h2>解析進捗</h2>
      
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '1rem' }}>
        {getStatusIcon()}
        <span className={`status-badge ${getStatusBadgeClass()}`}>
          {getStatusText()}
        </span>
        <span style={{ color: '#666' }}>- {analysis.fileName}</span>
      </div>

      {analysis.progress && (
        <>
          <div className="progress-bar">
            <div 
              className="progress-fill" 
              style={{ width: `${analysis.progress.percentage}%` }}
            ></div>
          </div>
          
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span style={{ fontSize: '0.9rem', color: '#666' }}>
              {getCurrentStepText()}: {analysis.progress.message}
            </span>
            <span style={{ fontSize: '0.9rem', fontWeight: 'bold', color: '#667eea' }}>
              {analysis.progress.percentage}%
            </span>
          </div>
        </>
      )}

      {analysis.error && (
        <div className="error-message" style={{ marginTop: '1rem' }}>
          <XCircle size={20} style={{ marginRight: '0.5rem' }} />
          {analysis.error}
        </div>
      )}

      {analysis.status === 'Running' && (
        <div style={{ fontSize: '0.9rem', color: '#666', marginTop: '1rem' }}>
          <p>📄 Document Intelligence による高精度な文書解析</p>
          <p>🤖 OpenAI Service による意味理解とリース判定</p>
          <p>📊 IFRS 16/ASC 842 準拠の総合レポート生成</p>
        </div>
      )}
    </section>
  );
};

export default AnalysisProgress;