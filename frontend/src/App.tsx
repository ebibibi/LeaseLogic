import React, { useState, useCallback } from 'react';
import FileUpload from './components/FileUpload';
import AnalysisProgress from './components/AnalysisProgress';
import AnalysisResults from './components/AnalysisResults';
import AnalysisHistory from './components/AnalysisHistory';
import { AnalysisState, AnalysisHistoryItem } from './types';
import { FileText } from 'lucide-react';

function App() {
  const [currentAnalysis, setCurrentAnalysis] = useState<AnalysisState | null>(null);
  const [analysisHistory, setAnalysisHistory] = useState<AnalysisHistoryItem[]>([]);

  const handleAnalysisStart = useCallback((analysis: AnalysisState) => {
    setCurrentAnalysis(analysis);
  }, []);

  const handleAnalysisUpdate = useCallback((analysis: AnalysisState) => {
    setCurrentAnalysis(analysis);
  }, []);

  const handleAnalysisComplete = useCallback((analysis: AnalysisState) => {
    setCurrentAnalysis(analysis);
    
    // Add to history
    const historyItem: AnalysisHistoryItem = {
      analysisId: analysis.analysisId,
      fileName: analysis.fileName,
      timestamp: new Date(),
      status: analysis.status,
      isLease: analysis.result?.analysisResult?.isLease,
      confidence: analysis.result?.analysisResult?.confidence
    };
    
    setAnalysisHistory(prev => [historyItem, ...prev.slice(0, 9)]); // Keep last 10
  }, []);

  const handleHistoryItemClick = useCallback((item: AnalysisHistoryItem) => {
    // In a real app, you'd fetch the full analysis result here
    console.log('History item clicked:', item);
  }, []);

  return (
    <div className="container">
      <header className="header">
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '1rem' }}>
          <FileText size={48} />
          <div>
            <h1>LeaseLogic</h1>
            <p>契約書リース判定システム</p>
          </div>
        </div>
      </header>

      <FileUpload 
        onAnalysisStart={handleAnalysisStart}
        disabled={currentAnalysis?.status === 'Running'}
      />

      {currentAnalysis && (
        <>
          <AnalysisProgress 
            analysis={currentAnalysis}
            onUpdate={handleAnalysisUpdate}
            onComplete={handleAnalysisComplete}
          />
          
          {currentAnalysis.status === 'Completed' && currentAnalysis.result && (
            <AnalysisResults result={currentAnalysis.result} />
          )}
        </>
      )}

      {analysisHistory.length > 0 && (
        <AnalysisHistory 
          history={analysisHistory}
          onItemClick={handleHistoryItemClick}
        />
      )}
    </div>
  );
}

export default App;