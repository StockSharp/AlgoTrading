# Estratégia Tipu EA Multi-Período
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia recria a lógica central do Consultor Especializado Tipu no StockSharp. Substitui os indicadores proprietários Tipu Trend e Tipu Stops por uma combinação de médias móveis exponenciais (EMA), filtragem pelo Average Directional Index (ADX) e controles de risco com Average True Range (ATR). O sistema procura alinhamento de tendência entre um período superior (padrão 1 hora) e um período de sinal (padrão 15 minutos), depois gerencia a posição com um módulo de piramidagem de ponto de equilíbrio, lógica de trailing stop e take profit fixo opcional.

A implementação foca em instrumentos líquidos e com tendência, onde os sinais de momentum multi-período são confiáveis. O período superior define o contexto e filtra as fases de range, enquanto o período de sinal fornece as entradas reais.

## Assinaturas de dados
- Candles do período superior (padrão 1 hora) para tendência EMA e detecção de range ADX.
- Candles do período de sinal (padrão 15 minutos) para sinais de entrada, posicionamento de stop ATR e atualizações de gestão de trades.

## Lógica de trading
1. **Contexto do período superior**
   - Calcular EMAs rápida e lenta e detectar cruzamentos. Um cruzamento de alta produz um sinal de tendência de alta; um cruzamento de baixa produz um sinal de tendência de baixa.
   - Medir a força da tendência com ADX. Se o ADX estiver abaixo do limite configurado, o mercado é marcado como em range e nenhum novo trade é permitido.
   - Armazenar o timestamp do último sinal do período superior. A validade do sinal expira após um número configurável de minutos.
2. **Entradas no período de sinal**
   - Aguardar um cruzamento EMA no período de sinal **e** um sinal fresco do período superior na mesma direção enquanto o período superior não está em range.
   - Entradas compradas requerem que a EMA rápida cruze acima da EMA lenta; entradas vendidas requerem o oposto.
   - Antes de enviar uma nova ordem, a estratégia opcionalmente fecha a posição oposta (comportamento de reversão no sinal) e respeita o flag de cobertura.
   - A distância inicial do stop é definida como `ATR * AtrMultiplier` e limitada pelo parâmetro `MaxRiskPips`. As ordens são ignoradas se o risco necessário exceder esse limite.
3. **Gestão de risco**
   - **Take profit**: alvo fixo opcional baseado em `TakeProfitPips`.
   - **Trailing stop**: uma vez que o preço se move `TrailingStartPips` a favor, o stop segue o mercado com um offset de `TrailingCushionPips`.
   - **Modo sem risco**: quando habilitado, a estratégia move o stop para o ponto de equilíbrio após `RiskFreeStepPips` de lucro e adiciona volume adicional em passos de `PyramidIncrementVolume` até `PyramidMaxVolume` ser atingido. Cada passo de piramidagem também aperta o stop protetor.
   - As posições são fechadas imediatamente no sinal oposto se `CloseOnReverseSignal` for verdadeiro.

## Parâmetros
- `AllowHedging` – Permitir adicionar posições sem primeiro fechar o lado oposto.
- `CloseOnReverseSignal` – Nivelar a posição atual quando chegar um sinal oposto.
- `EnableTakeProfit`, `TakeProfitPips` – Habilitar e configurar a distância de take profit fixo em pips.
- `MaxRiskPips` – Distância máxima de stop permitida em pips. Previne entradas com risco inicial excessivo.
- `TradeVolume` – Tamanho de ordem base para a primeira posição.
- `EnableRiskFreePyramiding`, `RiskFreeStepPips`, `PyramidIncrementVolume`, `PyramidMaxVolume` – Controlar a lógica de piramidagem sem risco.
- `EnableTrailingStop`, `TrailingStartPips`, `TrailingCushionPips` – Configurar o comportamento do trailing stop.
- `HigherFastLength`, `HigherSlowLength`, `LowerFastLength`, `LowerSlowLength` – Comprimentos de EMA para detecção de tendência em ambos os períodos.
- `AdxLength`, `AdxThreshold` – Parâmetros ADX usados para filtrar mercados em range no período superior.
- `AtrLength`, `AtrMultiplier` – Parâmetros ATR para cálculo do stop inicial.
- `HigherSignalWindowMinutes` – Período de validade para o sinal do período superior.
- `HigherCandleType`, `LowerCandleType` – Tipos/períodos de candles para processamento de contexto e sinal.

## Notas de comportamento
- O preço médio de entrada é recalculado sempre que novo volume é adicionado, garantindo que trailing stops e o módulo sem risco referenciem a base de custo real da posição.
- Todas as decisões de negociação são tomadas apenas em candles completados; candles incompletos são ignorados para evitar sinais prematuros.
- A estratégia emite ordens a mercado (`BuyMarket`/`SellMarket`) e realiza o gerenciamento de posições internamente sem depender de ordens stop pendentes.
- Como os indicadores Tipu originais são proprietários, combinações EMA/ADX/ATR são usadas como uma aproximação fiel, mantendo as funcionalidades originais de gestão de trades (reversão no sinal, piramidagem de ponto de equilíbrio e trailing stop).

## Dicas de uso
- Otimizar comprimentos de EMA, multiplicador ATR e limite ADX para o instrumento alvo; os padrões fornecidos funcionam como ponto de partida genérico para pares de FX principais.
- Definir `HigherSignalWindowMinutes` próximo à duração do período superior para exigir alinhamento quase sincronizado, ou aumentá-lo para permitir mais defasagem entre os sinais do período superior e inferior.
- Quando a piramidagem está desabilitada, a estratégia ainda move o stop para o ponto de equilíbrio uma vez atingida a distância `RiskFreeStepPips`, fornecendo proteção básica de risco.
- Desabilitar `CloseOnReverseSignal` se preferir gerenciar saídas manualmente ou permitir que o trailing stop gerencie todo o trade.
