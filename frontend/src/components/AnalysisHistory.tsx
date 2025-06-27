import React from 'react';
import { FileText, CheckCircle, XCircle, Clock } from 'lucide-react';
import { AnalysisHistoryItem } from '../types';

interface AnalysisHistoryProps {
  history: AnalysisHistoryItem[];
  onItemClick: (item: AnalysisHistoryItem) => void;
}

const AnalysisHistory: React.FC<AnalysisHistoryProps> = ({ history, onItemClick }) => {
  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'Completed':
        return <CheckCircle size={16} style={{ color: '#28a745' }} />;
      case 'Failed':
        return <XCircle size={16} style={{ color: '#dc3545' }} />;
      case 'Running':
        return <Clock size={16} style={{ color: '#ffc107' }} />;
      default:
        return <FileText size={16} style={{ color: '#6c757d' }} />;
    }
  };

  const getStatusText = (status: string) => {
    const statusMap: { [key: string]: string } = {
      'Completed': '完了',
      'Failed': '失敗',
      'Running': '実行中',
      'Terminated': '中断'
    };
    return statusMap[status] || status;
  };

  const formatDate = (date: Date): string => {
    return date.toLocaleString('ja-JP', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const getLeaseStatusText = (isLease?: boolean): string => {
    if (isLease === undefined) return '';
    return isLease ? 'リース' : '非リース';
  };

  if (history.length === 0) {
    return null;
  }

  return (
    <section className="history-section">
      <h2>解析履歴</h2>
      
      <div>
        {history.map((item) => (
          <div 
            key={item.analysisId} 
            className="history-item"
            onClick={() => onItemClick(item)}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
              <FileText size={20} style={{ color: '#667eea' }} />
              <div>
                <div className="history-filename">{item.fileName}</div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginTop: '0.25rem' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}>
                    {getStatusIcon(item.status)}
                    <span style={{ fontSize: '0.8rem', color: '#666' }}>
                      {getStatusText(item.status)}
                    </span>
                  </div>
                  {item.isLease !== undefined && (
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}>
                      <div className={`lease-indicator ${item.isLease ? 'is-lease' : 'not-lease'}`}></div>
                      <span style={{ fontSize: '0.8rem', color: '#666' }}>
                        {getLeaseStatusText(item.isLease)}
                      </span>
                    </div>
                  )}
                  {item.confidence !== undefined && (
                    <span style={{ fontSize: '0.8rem', color: '#667eea', fontWeight: 'bold' }}>
                      {(item.confidence * 100).toFixed(0)}%
                    </span>
                  )}
                </div>
              </div>
            </div>
            
            <div className="history-date">
              {formatDate(item.timestamp)}
            </div>
          </div>
        ))}
      </div>

      <div style={{ fontSize: '0.85rem', color: '#888', marginTop: '1rem', textAlign: 'center' }}>
        最新10件の解析履歴を表示しています
      </div>
    </section>
  );
};

export default AnalysisHistory;