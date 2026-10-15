# Estratégia de BladeRunner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia BladeRunner é uma tradução do consultor especialista de MetaTrader que combina rupturas de fractais com confirmação de tendência e momentum. O port do StockSharp mantém a estrutura multi-timeframe do script original, analisando três feeds de dados de velas diferentes: uma série primária para execução de operações, uma série de período superior para o filtro de momentum e uma série lenta para o filtro de tendência MACD. As ordens são abertas com escala configurável, stop-loss e distâncias de take-profit expressas em passos de preço.

## Lógica de trading
1. **Filtro de ruptura de fractal** – a estratégia escaneia velas completadas em busca de padrões de fractal de Bill Williams. Um fractal de alta (superior) é aceito quando a vela formada duas barras antes faz uma nova máxima de swing e a barra de confirmação abre abaixo do preço do fractal e da LWMA de 20 períodos do preço típico. Fractais de baixa aplicam as regras simétricas.
2. **Confirmação de tendência** – médias móvias ponderadas lineares (LWMA) rápida e lenta calculadas na série de velas primária definem a tendência subjacente. Posições compradas exigem que a LWMA rápida esteja acima da lenta, enquanto posições vendidas exigem o alinhamento oposto.
3. **Filtro de momentum** – um oscilador de momentum calculado no fluxo de velas de período superior deve desviar de 100 pelo menos pelo limiar configurado em qualquer uma das três últimas observações. Isso reproduz as verificações de spike de momentum da versão MQL.
4. **Filtro MACD** – um MACD calculado no período lento deve ter sua linha principal acima (comprado) ou abaixo (vendido) da linha de sinal, refletindo o filtro mensal usado pelo consultor especialista.
5. **Confirmação de ruptura** – o fechamento da vela primária mais recente deve romper além do nível fractal armazenado antes que a ordem seja enviada.

Quando todos os filtros se alinham, a estratégia abre uma posição de mercado usando o tamanho de lote configurado. A exposição existente na direção oposta é fechada antes de reverter. Entradas adicionais são permitidas até que o número máximo de operações de escala seja atingido.

## Detalhes de implementação
- Três assinaturas de velas são criadas via API de alto nível. Cada feed se vincula diretamente aos indicadores necessários sem adicioná-los à coleção global de indicadores.
- As LWMAs operam sobre o preço típico (HLC/3) para corresponder à implementação MQL. O MACD também consome preços típicos.
- A detecção de fractais armazena uma janela deslizante de velas completadas e valores de filtro associados. Apenas a direção de fractal validada mais recente é mantida, o que evita sinais duplicados na mesma estrutura.
- O histórico de momentum é mantido como um array de tamanho fixo, evitando alocações dinâmicas enquanto reproduz o look-back do EA original.
- O dimensionamento de ordens respeita as restrições da bolsa através de ajustes de passo de volume, volume mínimo e máximo.
- O helper integrado `StartProtection` aplica distâncias de stop-loss e take-profit expressas em passos de preço, correspondendo aos valores de pip fixos do MetaTrader.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Série de velas primária para geração de sinais. | Velas de 15 minutos |
| `MomentumCandleType` | Série de período superior para o filtro de momentum. | Velas de 1 hora |
| `MacdCandleType` | Série de velas para o filtro de tendência MACD. | Velas diárias |
| `FastMaPeriod` | Comprimento da LWMA rápida. | 6 |
| `SlowMaPeriod` | Comprimento da LWMA lenta. | 85 |
| `FilterMaPeriod` | LWMA para validar rupturas de fractal. | 20 |
| `MomentumPeriod` | Período de média do indicador de momentum. | 14 |
| `MomentumThreshold` | Desvio absoluto mínimo do momentum em relação a 100. | 0.3 |
| `FractalLookback` | Número de velas retidas para análise de fractais. | 200 |
| `MaxTrades` | Número máximo de ordens de escala por direção. | 3 |
| `OrderVolume` | Volume base para cada ordem de mercado. | 1 contrato |
| `TakeProfitSteps` | Distância de take-profit em passos de preço. | 50 |
| `StopLossSteps` | Distância de stop-loss em passos de preço. | 20 |

## Gestão de risco
- Níveis de stop-loss e take-profit são anexados automaticamente a cada posição via `StartProtection`.
- A estratégia sempre fecha a exposição oposta antes de abrir operações na nova direção para evitar situações de hedge.
- O volume é ajustado às restrições do instrumento antes de colocar ordens. O limite `MaxTrades` limita os passos de escala totais por direção.

## Diferenças do EA original
- As utilidades de stop de equity, trailing stop e break-even do MetaTrader não estão implementadas. O controle de risco do StockSharp pode ser adicionado externamente se necessário.
- A lógica de trailing baseada em dinheiro e notificações push são omitidas porque o StockSharp fornece fluxos de trabalho de notificação alternativos.
- O filtro MACD usa velas diárias por padrão em vez de barras mensais. Ajuste `MacdCandleType` para um período mensal quando suportado pela fonte de dados conectada.
- A validação de fractais depende da última vela de confirmação armazenada na janela deslizante. Isso produz o mesmo efeito prático que o loop no script MQL enquanto evita varreduras repetidas.

## Notas de uso
1. Configure os tipos de velas para corresponder aos instrumentos e períodos suportados pela sua fonte de dados.
2. Alinhe `OrderVolume`, `TakeProfitSteps` e `StopLossSteps` com o tamanho do tick e o passo de volume do instrumento.
3. Ajuste `MomentumThreshold` e os comprimentos de LWMA durante testes walk-forward para adaptar a sensibilidade de ruptura a diferentes mercados.
4. Habilite o desenho no gráfico para visualizar as três LWMAs e verificar se as rupturas de fractal se alinham com os filtros de tendência antes de operar ao vivo.
