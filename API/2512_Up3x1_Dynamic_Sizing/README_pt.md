# Estratégia Up3x1 com Dimensionamento Dinâmico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
- Conversão do consultor especializado do MetaTrader 5 `up3x1.mq5` para a API de alto nível do StockSharp.
- Opera um cruzamento triplo de médias móveis exponenciais (EMA) com gestão de stop loss, take profit e trailing stop.
- Processa apenas velas concluídas para emular a proteção original `iTickVolume(0) > 1` que forçava uma decisão por barra.
- A série de velas padrão é 1 hora, mas o período é configurável através do parâmetro `CandleType`.

## Lógica de Trading
1. **Indicadores**
   - EMA Rápida (`FastPeriod`, padrão 24).
   - EMA Média (`MediumPeriod`, padrão 60).
   - EMA Lenta (`SlowPeriod`, padrão 120).
2. **Entrada comprada**
   - Barra anterior: EMA rápida abaixo da EMA média e a média abaixo da lenta (`EMAfast₍t-1₎ < EMAmedium₍t-1₎ < EMAslow₍t-1₎`).
   - Barra atual: a EMA média abaixo da EMA rápida enquanto a rápida permanece abaixo da lenta (`EMAmedium₍t₎ < EMAfast₍t₎ < EMAslow₍t₎`).
3. **Entrada vendida**
   - Barra anterior: EMA rápida acima da EMA média e a média acima da lenta (`EMAfast₍t-1₎ > EMAmedium₍t-1₎ > EMAslow₍t-1₎`).
   - Barra atual: a EMA média cruza acima da EMA rápida enquanto ambas permanecem acima da EMA lenta (`EMAmedium₍t₎ > EMAfast₍t₎ > EMAslow₍t₎`).
4. **Lógica de saída para ambas as direções**
   - Take profit quando o preço avança `TakeProfitOffset` a partir da entrada (usando a máxima da vela para comprados, a mínima para vendidos).
   - Stop loss quando o preço recua `StopLossOffset` a partir da entrada (usando a mínima da vela para comprados, a máxima para vendidos).
   - O trailing stop se ativa assim que a posição se move favoravelmente por mais de `TrailingStopOffset` e então segue o preço a essa distância fixa, avaliada nas extremidades da vela.
   - Saída de fallback quando a EMA rápida cruza de volta abaixo da EMA média enquanto ambas permanecem acima da EMA lenta (espelha a verificação `ma_one_1 > ma_two_1 > ma_three_1` da versão MQL).

## Dimensionamento de Posição e Gestão de Risco
- `RiskFraction` (padrão 0.02) multiplica o valor atual do portfólio para aproximar o dimensionamento de lotes original `FreeMargin * 0.02 / 1000`.
- `BaseVolume` (padrão 0.1) atua como fallback quando os dados do portfólio não estão disponíveis ou o tamanho calculado não é positivo.
- Após mais de uma saída perdedora, o volume é reduzido por `volume * losses / 3`, imitando o contador cumulativo `losses` do script (o contador não é reiniciado após trades lucrativos, como no código original).
- Os volumes são arredondados para baixo para `Security.VolumeStep`, limitados por `Security.MinVolume` / `Security.MaxVolume`, e reduzidos a zero se o mínimo do instrumento não puder ser cumprido.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `FastPeriod` | 24 | Comprimento da EMA mais rápida. |
| `MediumPeriod` | 60 | Comprimento da EMA média. |
| `SlowPeriod` | 120 | Comprimento da EMA lenta usada como filtro de tendência de longo prazo. |
| `TakeProfitOffset` | 0.015 | Distância de preço absoluta para a ordem de take profit (adaptar à cotação do instrumento). |
| `StopLossOffset` | 0.01 | Distância de preço absoluta para a ordem de stop loss. |
| `TrailingStopOffset` | 0.004 | Distância de trailing que bloqueia ganhos assim que o preço avança suficientemente; definir como 0 para desabilitar. |
| `BaseVolume` | 0.1 | Tamanho de trade de fallback quando o dimensionamento dinâmico não pode ser calculado. |
| `RiskFraction` | 0.02 | Fração do valor do portfólio aplicada à fórmula de dimensionamento dinâmico. |
| `CandleType` | Período de 1 hora | Série de velas usada para cálculos de indicadores e tomada de decisão. |

## Notas de Conversão
- O trailing stop e as saídas de proteção usam máximas/mínimas de velas em vez de ticks brutos porque a API de alto nível processa velas concluídas; isso mantém o comportamento determinístico através de backtests e execuções ao vivo.
- O stop loss e take profit são executados via comandos de achatamento a mercado no limiar avaliado em vez de colocar ordens de proteção separadas, garantindo compatibilidade com o fluxo de estratégia de alto nível.
- O dimensionamento dinâmico de posição depende de `Portfolio.CurrentValue`. Quando indisponível, a estratégia recorre a `BaseVolume`, similar ao fallback `LotCheck` para a entrada manual `Lots` no original.
- O contador `losses` é intencionalmente cumulativo (nunca é reiniciado em trades vencedores) para seguir a implementação MQL.
- Todos os comentários estão em inglês conforme as diretrizes do projeto.

## Dicas de Uso
1. Annexe a estratégia a um instrumento e portfólio, então configure `CandleType` para corresponder à resolução do gráfico que você quer emular do MT5.
2. Revise os offsets de preço para que reflitam o tamanho do tick do seu instrumento (por exemplo, para um par Forex de 5 dígitos, 0.015 equivale a 150 pontos como no expert fonte).
3. Ajuste `RiskFraction` / `BaseVolume` para alcançar tamanhos de posição realistas em relação à sua conta.
4. Opcional: desabilite o trailing definindo `TrailingStopOffset` como zero.
5. Monitore logs para mensagens como "Enter long" ou "Exit short" que espelham os diagnósticos `Print` do MetaTrader.

## Estrutura do Repositório
```
API/2512_Up3x1/
├── CS/Up3x1DynamicSizingStrategy.cs      # Estratégia C# convertida
├── README.md                # Documentação em inglês (este arquivo)
├── README_zh.md             # Tradução para o chinês
└── README_ru.md             # Tradução para o russo
```

## Aviso Legal
O trading envolve risco significativo. Este exemplo é fornecido para fins educativos e deve ser validado em dados históricos e simulados antes de qualquer implantação ao vivo.
