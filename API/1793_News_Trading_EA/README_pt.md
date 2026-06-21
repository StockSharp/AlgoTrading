# Estratégia de Trading de Notícias EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia straddle baseada em tempo, projetada para operar em torno de divulgações de notícias econômicas. Em um horário programado, a estratégia coloca ordens simétricas de compra stop e venda stop a uma distância fixa do preço atual. As ordens são atualizadas a cada vela durante a janela de ativação para seguir o preço de mercado. Se uma posição for aberta, a ordem pendente oposta é cancelada e níveis opcionais de take-profit e stop-loss gerenciam as saídas.

## Detalhes

- **Critérios de entrada**:
  - Durante a janela straddle, colocar compra stop em close + Distance * step e venda stop em close - Distance * step.
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Stop oposto, take-profit/stop-loss ou expiração da ordem
- **Stops**: Stop loss e take profit fixos
- **Valores padrão**:
  - `StartDateTime` = DateTime.Now
  - `StartStraddle` = 0
  - `StopStraddle` = 15
  - `Volume` = 0.01m
  - `Distance` = 55m
  - `TakeProfit` = 30m
  - `StopLoss` = 30m
  - `Expiration` = 20
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: News
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Evento
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
