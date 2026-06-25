# Estratégia AltrTrend Signal v2.2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um porte do StockSharp do assessor especializado do MetaTrader **Exp_AltrTrend_Signal_v2_2**. Ela recria a
lógica de canal adaptativo do indicador AltrTrend Signal original e executa operações em barras atrasadas assim como a versão
MQL5. O valor ADX contrai ou expande o canal para que rompimentos só disparem quando a força de tendência os suporta.

## Como funciona

1. Um canal dinâmico é calculado em cada vela completada do período configurado. A largura do canal é definida pelo preço
   máximo e mínimo dentro de um lookback que se expande ou contrai de acordo com o valor ADX anterior (`KPeriod / ADX`).
2. Os limites internos (`smin`, `smax`) são puxados em direção ao centro em `KPercent`. O preço deve fechar fora desses limites
   internos para estabelecer um estado de tendência direcional.
3. Quando a tendência muda de baixista para altista e o fechamento está acima do limite superior, um sinal de compra é gerado.
   Uma reversão baixista abaixo do limite inferior emite um sinal de venda. Os sinais são executados na barra definida pelo
   atraso `SignalBar`, correspondendo ao comportamento do assessor especializado original.
4. Níveis opcionais de stop-loss e take-profit são mapeados de pontos para passos de preço para que as saídas de proteção
   imitem a colocação de ordem original com valores fixos de SL/TP.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A tendência anterior era baixista ou neutra, o preço fecha acima do limite superior contraído e as entradas
    compradas estão habilitadas. As posições vendidas podem ser fechadas automaticamente se permitido.
  - **Vendido**: A tendência anterior era altista ou neutra, o preço fecha abaixo do limite inferior contraído e as entradas
    vendidas estão habilitadas. As posições compradas podem ser fechadas automaticamente se permitido.
- **Critérios de saída**:
  - Sinal de rompimento oposto quando as saídas são permitidas para a direção atual.
  - Distâncias de stop-loss ou take-profit expressas em passos de preço.
- **Comprado/Vendido**: Direção dupla com interruptores independentes de habilitação/desabilitação para entradas e saídas.
- **Gestão de risco**:
  - `StopLossPoints` e `TakeProfitPoints` replicam o módulo MM original aplicando saídas baseadas em distância após ordens de
    mercado serem preenchidas.
- **Configurações do indicador**:
  - `KPercent` controla quanto as bordas do canal são contraídas em direção ao intervalo médio.
  - `KStop` mantém o valor de projeção de seta original para gráficos e logging.
  - `KPeriod` é o lookback base antes da modulação ADX.
  - `AdxPeriod` define o comprimento do Average Directional Index que adapta a largura do canal.
  - `SignalBar` atrasa a execução de ordens pelo número especificado de velas completadas.
- **Mercados recomendados**:
  - Funciona melhor em instrumentos com fases de swing claras onde a força de tendência varia ao longo do tempo (principais pares
    de forex, ouro e futuros de índices). O período padrão é H1 como no modelo MQL5.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `CandleType` | Período usado para construir o canal adaptativo. |
| `KPercent` | Porcentagem que puxa os limites internos do canal para dentro. |
| `KStop` | Multiplicador para preços de seta projetados (mantido para compatibilidade). |
| `KPeriod` | Número base de velas examinadas antes do ajuste ADX. |
| `AdxPeriod` | Período do Average Directional Index que impulsiona a largura do canal. |
| `SignalBar` | Número de velas completadas para aguardar antes de executar um sinal. |
| `AllowBuyEntries` / `AllowSellEntries` | Habilitar ou desabilitar a abertura de posições em cada direção. |
| `AllowBuyExits` / `AllowSellExits` | Permitir o fechamento automático de posições em sinais opostos. |
| `StopLossPoints` | Distância do stop-loss medida em passos de preço (0 desabilita). |
| `TakeProfitPoints` | Distância do take-profit medida em passos de preço (0 desabilita). |

Este porte mantém os interruptores discricionários e os parâmetros de risco do assessor especializado original, facilitando a
reprodução do mesmo comportamento dentro do StockSharp Designer, Shell ou Runner.
