# Estratégia de Padrão Engolfamento Baixista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Este padrão visa capturar o início de um movimento de baixa após uma recuperação. Um engolfamento baixista ocorre quando uma vela vermelha envolve completamente o corpo altista anterior. Contar algumas barras consecutivas de alta antes do padrão garante que o mercado estava subindo anteriormente.

Os testes indicam um retorno anual médio de aproximadamente 79%. Tem melhor desempenho no mercado de ações.

O algoritmo armazena cada vela em sequência. Se a nova barra fechar abaixo da abertura e seu corpo envolver a barra altista anterior, uma venda a descoberto é executada. O stop-loss é posicionado acima da máxima do padrão para limitar a exposição.

As posições são geralmente gerenciadas com o stop protetor, embora o trader possa sair manualmente se as condições mudarem. Exigir uma tendência de alta ajuda a evitar sinais falsos em mercados irregulares.

## Detalhes

- **Critérios de entrada**: Vela baixista envolve a barra altista anterior, com tendência de alta opcional presente.
- **Comprado/Vendido**: Somente vendido.
- **Critérios de saída**: Stop-loss ou discrecionário.
- **Stops**: Sim, acima da máxima do padrão.
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 1
  - `RequireUptrend` = true
  - `UptrendBars` = 3
- **Filtros**:
  - Categoria: Padrão
  - Direção: Vendido
  - Indicadores: Candlestick
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

