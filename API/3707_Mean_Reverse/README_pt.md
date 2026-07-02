# Estratégia reversa média
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Mean Reverse replica o consultor especialista "MeanReversionTrendEA". Ele combina um módulo de tendência de cruzamento de média móvel com uma sobreposição de reversão à média impulsionada por bandas de volatilidade Average True Range (ATR). A ideia é abrir uma posição quando o preço confirmar uma mudança de tendência de alta ou baixa ou se afastar muito da média móvel mais lenta por uma distância ajustada pela volatilidade.

## Lógica de negociação
- **Componente de tendência**: uma configuração longa aparece quando a média móvel simples rápida (SMA) cruza acima da lenta SMA. Uma configuração curta é acionada quando o SMA rápido cruza abaixo do SMA lento.
- **Componente de reversão à média**: uma configuração longa é ativada sempre que o preço de fechamento cai abaixo do lento SMA em mais de `ATR × Multiplier`. Uma configuração curta aparece quando o preço sobe acima do lento SMA por mais do que a mesma distância.
- **Combinação de sinais**: se o módulo de tendência ou o módulo de reversão à média sinalizar uma posição comprada (curta) enquanto nenhuma posição estiver aberta, a estratégia entra em uma posição comprada (curta) com o volume configurado.

## Gestão Comercial
- **Stop-loss**: imediatamente após a entrada a estratégia coloca um nível de preço em `entry − StopLossPoints × Step` para posições longas ou `entry + StopLossPoints × Step` para posições curtas. Quando os extremos da vela tocam este nível, a posição é fechada.
- **Take-profit**: uma meta de lucro é colocada em `entry + TakeProfitPoints × Step` para negociações longas ou `entry − TakeProfitPoints × Step` para negociações curtas. Um toque na respectiva máxima ou mínima da vela fecha a posição.
- **Restrição de posição única**: o algoritmo mantém no máximo uma posição aberta. Novos sinais são ignorados até que a negociação atual seja fechada.
- **Módulo de segurança**: a chamada `StartProtection()` integrada espelha a camada de validação de negociação de segurança do consultor especialista original e protege contra estados de posição inesperados.

## Indicadores
- **Média Móvel Simples (SMA)** com período `FastMaPeriod`.
- **Média Móvel Simples (SMA)** com período `SlowMaPeriod`.
- **Intervalo verdadeiro médio (ATR)** com período `AtrPeriod`.

Todos os indicadores são atualizados a partir da mesma assinatura de vela definida por `CandleType`.

## Parâmetros
| Nome | Descrição | Padrão |
|------|-------------|---------|
| `FastMaPeriod` | Lookback do SMA rápido usado na detecção de tendências e nas bandas de reversão à média. | 20 |
| `SlowMaPeriod` | Lookback do lento SMA que representa a média de equilíbrio. | 50 |
| `AtrPeriod` | Número de velas para cálculo de volatilidade ATR. | 14 |
| `AtrMultiplier` | Multiplicador aplicado a ATR para verificações de distância. | 2,0 |
| `StopLossPoints` | Distância de stop-loss medida em `Security.Step` unidades. | 500 |
| `TakeProfitPoints` | Distância de realização de lucro medida em `Security.Step` unidades. | 1000 |
| `TradeVolume` | Volume enviado com cada ordem de mercado. | 1 |
| `CandleType` | Tipo de dados Candle que alimenta os indicadores. | Período de 1 hora |

## Notas
- O tamanho padrão da vela é de uma hora para refletir a lógica do "período atual" da versão MetaTrader. Ajuste-o para corresponder ao período original do gráfico.
- Os envelopes baseados em ATR usam o fechamento da vela como preço de referência, refletindo o ponto médio original entre o lance e o pedido.
- Use os sinalizadores de otimização anexados aos parâmetros para calibrar o sistema para diferentes mercados.
