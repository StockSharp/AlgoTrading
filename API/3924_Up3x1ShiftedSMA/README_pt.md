# Estratégia Up3x1 Shifted SMA (conversão MT4)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Conversão do MetaTrader 4 consultor especialista `up3x1.mq4` localizado em `MQL/8097`.
- Implementa o cruzamento triplo de média móvel simples com uma mudança positiva no gráfico exatamente como no script original.
- Processa apenas velas concluídas para emular a guarda `Volume[0] > 1` que forçou o especialista a avaliar uma vez por barra.
- Os recursos de gerenciamento de risco incluem takeprofit, stop loss, redução dinâmica de lote após perdas em negociações e um trailing stop opcional.

## Lógica de negociação
1. **Indicadores**
   - Três médias móveis simples com um deslocamento gráfico de 6 barras (rápido = 24, médio = 60, lento = 120 por padrão).
2. **Entrada longa**
   - Barra anterior: `SMAfast₍t-1₎ < SMAmedium₍t-1₎ < SMAslow₍t-1₎`.
   - Barra atual: `SMAmedium₍t₎ < SMAfast₍t₎ < SMAslow₍t₎`.
   - A condição replica `ma1 < ma2 < ma3 && ma5 < ma4 < ma6` de MQL.
3. **Entrada curta**
   - Barra anterior: `SMAfast₍t-1₎ > SMAmedium₍t-1₎ > SMAslow₍t-1₎`.
   - Barra atual: `SMAmedium₍t₎ > SMAfast₍t₎ > SMAslow₍t₎`.
4. **Regras de saída**
   - Take Profit e Stop Loss respeitam a distância do ponto configurado multiplicada por `Security.PriceStep` (ou usado diretamente quando o passo é desconhecido).
   - O trailing stop bloqueia os lucros quando o preço avança mais de `TrailingStopPoints` e segue o extremo alcançado após a entrada.
   - Saída à prova de falhas quando as médias móveis mudam para a ordem oposta, espelhando a lógica `OrderClose` original.

## Dimensionamento de posições
- O volume padrão é igual a `BaseVolume` (0,1 lote) sempre que as métricas do portfólio não estiverem disponíveis.
- Quando `Portfolio.CurrentValue` existe, a estratégia o multiplica por `RiskFraction` (padrão `0.00002`, equivalente à fórmula MQL `FreeMargin * 0.02 / 1000`).
- Após mais de uma saída perdida, o volume é reduzido em `volume * losses / 3`, exatamente como a rotina `LotsOptimized`.
- O volume é arredondado para `Security.VolumeStep` e reduzido a zero se não puder satisfazer `Security.MinVolume`.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `FastPeriod` | 24 | Comprimento do deslocamento mais rápido SMA. |
| `MediumPeriod` | 60 | Comprimento do meio deslocado SMA. |
| `SlowPeriod` | 120 | Comprimento do deslocamento lento SMA. |
| `TakeProfitPoints` | 150 | Distância em pontos de preço entre o preço de entrada e o lucro. |
| `StopLossPoints` | 100 | Distância em pontos de preço entre o preço de entrada e o stop loss. |
| `TrailingStopPoints` | 100 | Distância de parada móvel opcional em pontos (definida como 0 para desabilitar). |
| `BaseVolume` | 0,1 | Tamanho da negociação de reserva e volume mínimo após reduções. |
| `RiskFraction` | 0,00002 | Fração do valor do portfólio utilizado para calcular o volume dinâmico. |
| `CandleType` | Período de 1 hora | Série de velas usadas para alimentar indicadores. |

## Notas de conversão
- A estratégia usa o API de alto nível (`SubscribeCandles` + `Bind`) e evita buffers de histórico manuais.
- Os valores do indicador são armazenados entre chamadas para imitar o parâmetro `shift` sem acesso direto ao índice.
- As saídas protetoras são executadas com comandos de mercado no nível de preço detectado para permanecerem compatíveis com a abstração StockSharp.
- Todos os comentários in-line são escritos em inglês, em conformidade com as diretrizes do projeto.

## Uso
1. Anexe a estratégia a um título e portfólio no StockSharp Designer ou código.
2. Selecione uma série de velas (`CandleType`) que corresponda ao seu período MT4 (H1 por padrão).
3. Revise os parâmetros de risco baseados em pontos para alinhá-los com o tamanho do tick do instrumento (por exemplo, 0,0001 para a maioria dos pares Forex).
4. Defina `TrailingStopPoints` como zero quando o rastreamento não for necessário.
5. Monitore os registros em busca de mensagens como "Enter long" e "Exit short" que espelham o diagnóstico do MQL.

## Estrutura do Repositório
```
API/3924/
├── CS/Up3x1ShiftedSmaStrategy.cs # Estratégia C# convertida com comentários em inglês
├── README.md # Documentação em inglês (este arquivo)
├── README_zh.md # Tradução chinesa
└── README_ru.md # Tradução russa
```

## Isenção de responsabilidade
A negociação envolve riscos significativos. A estratégia é fornecida para fins educacionais e deve ser validada em dados históricos e simulados antes da negociação ao vivo.
