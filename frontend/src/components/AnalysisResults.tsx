import React from 'react';
import { 
  CheckCircle, 
  XCircle, 
  FileText, 
  TrendingUp, 
  AlertTriangle, 
  BookOpen,
  Clock,
  Info
} from 'lucide-react';
import { AnalysisResult } from '../types';

interface AnalysisResultsProps {
  result: AnalysisResult;
}

const AnalysisResults: React.FC<AnalysisResultsProps> = ({ result }) => {
  const { analysisResult, documentSummary, processingTime, completedAt } = result;
  
  const formatProcessingTime = (timeString: string): string => {
    // Parse the timespan format (HH:mm:ss)
    const parts = timeString.split(':');
    if (parts.length === 3) {
      const hours = parseInt(parts[0]);
      const minutes = parseInt(parts[1]);
      const seconds = parseInt(parts[2]);
      
      if (hours > 0) {
        return `${hours}時間${minutes}分${seconds}秒`;
      } else if (minutes > 0) {
        return `${minutes}分${seconds}秒`;
      } else {
        return `${seconds}秒`;
      }
    }
    return timeString;
  };

  const getLeaseTypeText = (leaseType: string): string => {
    const typeMap: { [key: string]: string } = {
      'OperatingLease': 'オペレーティングリース',
      'FinanceLease': 'ファイナンスリース',
      'ServiceContract': 'サービス契約',
      'NotApplicable': '該当なし'
    };
    return typeMap[leaseType] || leaseType;
  };

  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleString('ja-JP', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  return (
    <section className="results-section">
      <h2>解析結果</h2>
      
      <div className="result-card">
        <div className="result-header">
          <div className={`lease-indicator ${analysisResult.isLease ? 'is-lease' : 'not-lease'}`}></div>
          <h3 style={{ margin: 0 }}>
            {analysisResult.isLease ? 'リース契約' : 'リース契約ではない'}
          </h3>
          <div className="confidence-score">
            信頼度: {(analysisResult.confidence * 100).toFixed(1)}%
          </div>
        </div>

        <div style={{ marginBottom: '1rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
            <FileText size={16} />
            <strong>分類:</strong> {getLeaseTypeText(analysisResult.leaseType)}
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
            <Clock size={16} />
            <strong>処理時間:</strong> {formatProcessingTime(processingTime)}
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <Info size={16} />
            <strong>完了日時:</strong> {formatDate(completedAt)}
          </div>
        </div>

        <div style={{ background: '#f8f9fa', padding: '1rem', borderRadius: '6px', marginBottom: '1rem' }}>
          <h4 style={{ margin: '0 0 0.5rem 0', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <BookOpen size={16} />
            契約概要
          </h4>
          <p style={{ margin: 0, color: '#666', lineHeight: '1.5' }}>{documentSummary}</p>
        </div>

        <div className="result-details">
          <div className="detail-section">
            <h4>契約詳細</h4>
            <ul>
              <li><strong>契約種別:</strong> {analysisResult.summary.contractType}</li>
              <li><strong>主要資産:</strong> {analysisResult.summary.primaryAsset}</li>
              <li><strong>契約期間:</strong> {analysisResult.summary.contractPeriod}</li>
              <li><strong>月額支払:</strong> {analysisResult.summary.monthlyPayment}</li>
            </ul>
          </div>

          <div className="detail-section">
            <h4>
              <CheckCircle size={16} style={{ color: '#28a745', marginRight: '0.5rem' }} />
              主要な発見事項
            </h4>
            <ul>
              {analysisResult.keyFindings.map((finding, index) => (
                <li key={index}>{finding}</li>
              ))}
            </ul>
          </div>

          {analysisResult.riskFactors.length > 0 && (
            <div className="detail-section">
              <h4>
                <AlertTriangle size={16} style={{ color: '#ffc107', marginRight: '0.5rem' }} />
                リスク要因
              </h4>
              <ul>
                {analysisResult.riskFactors.map((risk, index) => (
                  <li key={index}>{risk}</li>
                ))}
              </ul>
            </div>
          )}

          <div className="detail-section">
            <h4>
              <TrendingUp size={16} style={{ color: '#17a2b8', marginRight: '0.5rem' }} />
              推奨事項
            </h4>
            <ul>
              {analysisResult.recommendations.map((recommendation, index) => (
                <li key={index}>{recommendation}</li>
              ))}
            </ul>
          </div>

          {analysisResult.complianceRequirements.length > 0 && (
            <div className="detail-section">
              <h4>
                <BookOpen size={16} style={{ color: '#6f42c1', marginRight: '0.5rem' }} />
                コンプライアンス要件
              </h4>
              <ul>
                {analysisResult.complianceRequirements.map((requirement, index) => (
                  <li key={index}>{requirement}</li>
                ))}
              </ul>
            </div>
          )}
        </div>

        {analysisResult.isLease && (
          <div style={{ marginTop: '1.5rem' }}>
            <h4>詳細リース分析</h4>
            
            <div className="result-details">
              <div className="detail-section">
                <h4>
                  {analysisResult.leaseAnalysis.identifiedAsset.hasIdentifiedAsset ? 
                    <CheckCircle size={16} style={{ color: '#28a745', marginRight: '0.5rem' }} /> :
                    <XCircle size={16} style={{ color: '#dc3545', marginRight: '0.5rem' }} />
                  }
                  特定された資産
                </h4>
                <p style={{ margin: '0.5rem 0', fontSize: '0.9rem' }}>
                  {analysisResult.leaseAnalysis.identifiedAsset.assetDescription}
                </p>
                <p style={{ margin: '0.5rem 0', fontSize: '0.85rem', color: '#666' }}>
                  {analysisResult.leaseAnalysis.identifiedAsset.assetSpecificity}
                </p>
                {analysisResult.leaseAnalysis.identifiedAsset.citations.length > 0 && (
                  <div style={{ fontSize: '0.8rem', color: '#888' }}>
                    参照: {analysisResult.leaseAnalysis.identifiedAsset.citations.join(', ')}
                  </div>
                )}
              </div>

              <div className="detail-section">
                <h4>
                  {analysisResult.leaseAnalysis.rightToControl.hasRightToControl ? 
                    <CheckCircle size={16} style={{ color: '#28a745', marginRight: '0.5rem' }} /> :
                    <XCircle size={16} style={{ color: '#dc3545', marginRight: '0.5rem' }} />
                  }
                  使用権の制御
                </h4>
                <ul style={{ margin: '0.5rem 0' }}>
                  {analysisResult.leaseAnalysis.rightToControl.controlIndicators.map((indicator, index) => (
                    <li key={index} style={{ fontSize: '0.9rem' }}>{indicator}</li>
                  ))}
                </ul>
                {analysisResult.leaseAnalysis.rightToControl.citations.length > 0 && (
                  <div style={{ fontSize: '0.8rem', color: '#888' }}>
                    参照: {analysisResult.leaseAnalysis.rightToControl.citations.join(', ')}
                  </div>
                )}
              </div>

              <div className="detail-section">
                <h4>
                  {analysisResult.leaseAnalysis.substantiveSubstitutionRights.hasSubstitutionRights ? 
                    <AlertTriangle size={16} style={{ color: '#ffc107', marginRight: '0.5rem' }} /> :
                    <CheckCircle size={16} style={{ color: '#28a745', marginRight: '0.5rem' }} />
                  }
                  実質的な代替権
                </h4>
                <p style={{ margin: '0.5rem 0', fontSize: '0.9rem' }}>
                  {analysisResult.leaseAnalysis.substantiveSubstitutionRights.analysis}
                </p>
                {analysisResult.leaseAnalysis.substantiveSubstitutionRights.citations.length > 0 && (
                  <div style={{ fontSize: '0.8rem', color: '#888' }}>
                    参照: {analysisResult.leaseAnalysis.substantiveSubstitutionRights.citations.join(', ')}
                  </div>
                )}
              </div>
            </div>
          </div>
        )}
      </div>
    </section>
  );
};

export default AnalysisResults;