# Estratégia Blau SM Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão em C# do expert original do MetaTrader 5 `Exp_BlauSMStochastic` construído em torno do oscilador Blau SM Stochastic. O indicador mede a distância entre o preço e o intervalo de trading recente, aplica múltiplos estágios de suavização e compara o resultado com uma linha de referência suavizada. A estratégia trabalha em velas concluídas (período de 4 horas por padrão) e permite o trading em ambas as direções.

## Lógica do indicador
1. Calcular o máximo mais alto e o mínimo mais baixo ao longo de `LookbackLength` barras.
2. Construir uma série de preço sem tendência: `sm = price - (HH + LL) / 2` onde `price` é o tipo de preço aplicado.
3. Suavizar a série sem tendência sequencialmente por três médias móveis com comprimentos `FirstSmoothingLength`, `SecondSmoothingLength` e `ThirdSmoothingLength` usando o `SmoothMethod` selecionado (SMA, EMA, SMMA ou LWMA).
4. Suavizar o meio-intervalo `(HH - LL) / 2` com a mesma sequência tripla para normalizar a volatilidade.
5. Formar a linha principal do oscilador como `100 * smoothed(sm) / smoothed(range)`.
6. Suavizar a linha principal com `SignalLength` para obter a linha de sinal.

O parâmetro `Phase` é mantido por compatibilidade com a versão MQL, mas não é utilizado pelo motor de suavização simplificado.

## Modos de trading
- **Breakdown**: monitora cruzamentos de zero da linha principal. Um cruzamento de positivo para não positivo abre uma compra e fecha vendas. Um cruzamento de negativo para não negativo abre uma venda e fecha compras.
- **Twist**: rastreia torções de momentum. Se a linha principal forma um mínimo local (o valor sobe após cair), uma entrada comprada é acionada, enquanto um máximo local (o valor cai após subir) aciona uma venda. As posições opostas são fechadas de acordo.
- **CloudTwist**: observa cruzamentos entre a linha principal e a linha de sinal. Um cruzamento descendente da linha principal através da linha de sinal abre uma compra e fecha vendas, enquanto um cruzamento ascendente abre uma venda e fecha compras.

Os interruptores de entrada e saída (`EnableLongEntry`, `EnableShortEntry`, `EnableLongExit`, `EnableShortExit`) permitem desabilitar operações específicas mantendo os cálculos do indicador intactos.

## Gestão de risco
`TakeProfitPoints` e `StopLossPoints` se convertem em distâncias de preço absolutas usando o passo de preço do instrumento e são passados ao bloco de proteção incorporado via `StartProtection`. Defina-os como zero para desabilitar o limite correspondente.

## Parâmetros
- `CandleType` *(DataType, padrão: período de 4 horas)* – período usado para assinatura de velas e cálculos de indicadores.
- `Mode` *(BlauSmStochasticModes, padrão: Twist)* – seleciona o modo de geração de sinais (Breakdown, Twist, CloudTwist).
- `SignalBar` *(int, padrão: 1)* – número de barras para deslocar valores do indicador ao avaliar sinais, reproduzindo a lógica `SignalBar` original.
- `LookbackLength` *(int, padrão: 5)* – barras usadas para calcular os valores mais altos e mais baixos.
- `FirstSmoothingLength` *(int, padrão: 20)* – comprimento do primeiro estágio de suavização.
- `SecondSmoothingLength` *(int, padrão: 5)* – comprimento do segundo estágio de suavização.
- `ThirdSmoothingLength` *(int, padrão: 3)* – comprimento do terceiro estágio de suavização.
- `SignalLength` *(int, padrão: 3)* – comprimento de suavização da linha de sinal.
- `SmoothMethod` *(BlauSmSmoothMethods, padrão: EMA)* – família de médias móveis aplicada a todos os estágios de suavização (SMA, EMA, SMMA, LWMA).
- `PriceType` *(BlauSmAppliedPrices, padrão: Close)* – preço aplicado usado para alimentar o oscilador (fechamento, abertura, máximo, mínimo, mediana, típico, ponderado, simples, quartil, variantes de seguimento de tendência, Demark).
- `EnableLongEntry` *(bool, padrão: true)* – permite a abertura de posições compradas.
- `EnableShortEntry` *(bool, padrão: true)* – permite a abertura de posições vendidas.
- `EnableLongExit` *(bool, padrão: true)* – permite o fechamento de posições compradas.
- `EnableShortExit` *(bool, padrão: true)* – permite o fechamento de posições vendidas.
- `TakeProfitPoints` *(int, padrão: 2000)* – distância de take-profit fixa expressa em pontos do instrumento.
- `StopLossPoints` *(int, padrão: 1000)* – distância de stop-loss fixa expressa em pontos do instrumento.

## Notas
- O motor de suavização atualmente suporta médias móveis clássicas (SMA, EMA, SMMA, LWMA). Modos exóticos da biblioteca MQL (JMA, JurX, etc.) não estão disponíveis no StockSharp e, portanto, não estão incluídos.
- Phase é preservado como parâmetro por completude; ajuste-o apenas para fins de documentação.
- Funciona com qualquer símbolo suportado pelo StockSharp. Ajuste o tipo de vela, os comprimentos de suavização e os stops para corresponder à volatilidade do instrumento.
