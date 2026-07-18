# Intervalo de tempo Twenty200
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma versão StockSharp do consultor especialista MetaTrader **20/200 expert v4.2 (AntS)**. Ele aguarda uma hora específica do dia de negociação e depois compara dois preços históricos de abertura por hora (6 e 2 barras na configuração padrão). Se a abertura distante for superior à abertura mais próxima em mais de `Short Delta` pips, a estratégia vende, enquanto o gap reverso que excede `Long Delta` pips abre uma posição longa.

## Lógica de negociação

- A estratégia assina velas horárias (configuráveis através de `Candle Type`).
- Apenas uma negociação por dia é permitida. Os pedidos são feitos quando uma vela com hora igual a `Trade Hour` se torna ativa.
- Os sinais usam o preço de abertura `LookbackFar` e `LookbackNear` barras atrás da vela atual.
  - **Configuração curta:** `Open[t1] - Open[t2] > Short Delta × pip`.
  - **Configuração longa:** `Open[t2] - Open[t1] > Long Delta × pip`.
- Uma ordem de mercado é enviada com o volume calculado. As distâncias de stop-loss e take-profit são retiradas da versão MetaTrader e expressas em pips, convertidas automaticamente em preços via `Security.PriceStep`.
- Apenas uma posição pode existir por vez. A negociação diária é retomada no próximo dia corrido.

## Gestão de posição

- Stop-loss e take-profit são avaliados em cada atualização da vela usando os extremos máximo/mínimo da vela.
- `Max Open Hours` força uma saída do mercado quando o tempo de vida da posição excede o número configurado de horas (504 horas por padrão). Defina o parâmetro como zero para desativar o temporizador de segurança.

## Gestão de dinheiro

- `Fixed Volume` define o tamanho do contrato substituto usado quando `Use Auto Lot` está desativado ou as informações de saldo não estão disponíveis.
- Quando `Use Auto Lot` está ativado, o tamanho do lote segue a enorme tabela de etapas do consultor especialista. Em StockSharp a tabela é aproximada por `volume = round(balance × Auto Lot Factor, 2)` com o fator padrão `0.000038`, reproduzindo os valores MT4 dentro de um pip de volume em toda a faixa documentada (300 USD a 270.000 USD+).
- Se o valor atual do portfólio cair abaixo do último saldo registrado, a próxima negociação será multiplicada por `Big Lot Multiplier`, imitando a negociação de recuperação "Big Lot" no código original.
- Os volumes são alinhados a `Security.VolumeStep` e limitados entre `MinVolume`/`MaxVolume` quando disponíveis.

## Diferenças versus MetaTrader EA

- O script MT4 armazenou mais de mil linhas de limite manual. A versão StockSharp usa um coeficiente linear (`Auto Lot Factor`) que se ajusta à mesma escada. Ajuste o fator se precisar de uma réplica exata para um corretor diferente.
- As ordens stop-loss/take-profit são simuladas através de saídas de mercado nos extremos das velas. Isso mantém o comportamento consistente em backtests e negociações ao vivo, sem depender do suporte de ordens stop do lado da bolsa.
- Variáveis globais (`globalBalans`, `globalPosic`) são substituídas pelo estado na memória. Nenhum sistema de arquivos ou estado de terminal é necessário.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| Lucro Longo/Curto | Distância em pips para metas de lucro. |
| Stop Loss Longo/Curto | Distância em pips para stop loss. |
| Horário comercial | Hora da sessão (0–23) em que os sinais podem ser acionados. |
| Lookback distante/perto | Quantas barras voltam para inspecionar os dois preços de abertura. |
| Delta longo/curto | Gap de pip necessário para abrir uma posição. |
| Horário máximo de abertura | Vida útil máxima da posição em horas (0 desativa a proteção). |
| Volume Fixo | Volume de contrato de linha de base quando o dimensionamento automático está desabilitado. |
| Usar lote automático | Habilite o dimensionamento do lote a partir do valor da conta. |
| Fator de lote automático | Multiplicador aplicado ao valor do portfólio para emular a tabela de passos MT4. |
| Multiplicador de lote grande | Multiplicador de volume aplicado após uma queda no patrimônio. |
| Tipo de vela | Período usado para as velas de sinalização. |
