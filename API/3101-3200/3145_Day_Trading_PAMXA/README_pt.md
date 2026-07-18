# Estratégia de Day Trading PAMXA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Descrição
A estratégia **Day Trading PAMXA** reproduz o consultor especialista do MetaTrader 5 que combina reversões de momentum do Awesome Oscillator de Bill Williams com um filtro estocástico. O port para StockSharp mantém o design multi-período original:

- O loop de decisão principal é executado no período de **Velas de Sinal** (padrão 1 hora).
- O Awesome Oscillator é avaliado em um período separado de **Velas AO** (padrão 1 dia) para obter momentum de período superior.
- O oscilador estocástico usa seu próprio período de **Velas Estocásticas** (padrão 1 hora) para que os níveis %K/%D estejam alinhados com as configurações originais.

A estratégia mantém no máximo uma posição por vez. Quando aparece uma configuração altista, primeiro cobre quaisquer posições vendidas ativas antes de entrar comprado, e vice-versa para configurações baixistas.

## Lógica de Entrada
1. Calcular os valores terminados mais recentes do Awesome Oscillator no período AO.
2. Calcular os valores terminados mais recentes de %K e %D do oscilador estocástico no período estocástico.
3. Em cada vela de sinal terminada:
   - **Configuração altista**: acionada quando a barra AO anterior estava abaixo de zero e a última barra fechou acima de zero (reversão de momentum) enquanto %K ou %D estiver abaixo do limiar `Nível estocástico baixo` (condição de sobrevenda). Qualquer posição vendida aberta é coberta e uma nova posição comprada é aberta se não restar posição.
   - **Configuração baixista**: acionada quando a barra AO anterior estava acima de zero e a última barra fechou abaixo de zero enquanto %K ou %D estiver acima do limiar `Nível estocástico alto` (condição de sobrecompra). Qualquer posição comprada aberta é fechada e, se flat, uma nova posição vendida é aberta.

## Saída e Gestão de Risco
- Um **stop-loss** e **take-profit** baseados em pips são anexados na entrada. Quando a mínima da vela (para comprados) ou a máxima (para vendidos) viola o nível de stop, a posição é liquidada imediatamente. A mesma lógica se aplica ao alvo de lucro.
- Um **trailing stop** opcional ativa quando o preço avançou `Trailing Stop + Trailing Step` pips a favor da posição. Para comprados o stop segue a máxima mais alta menos a distância de trailing; para vendidos segue a mínima mais baixa mais a distância de trailing. O ajuste de trailing ocorre somente quando o movimento excede o passo de trailing.
- A gestão monetária pode operar em dois modos:
  - `FixedVolume`: usa o parâmetro `Order Volume` diretamente.
  - `RiskPercent`: calcula o volume de modo que o percentual configurado do valor do portfólio seria perdido se o stop-loss for atingido.
- A estratégia nunca piramida – uma vez que existe uma posição, o próximo sinal oposto irá achatá-la antes de qualquer nova entrada ser considerada.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `Stop Loss` | Distância de stop-loss em pips. Zero desativa o stop protetor.
| `Take Profit` | Distância de take-profit em pips. Zero desativa o alvo de lucro.
| `Trailing Stop` | Distância de ativação do trailing stop em pips. Zero desativa o trailing.
| `Trailing Step` | Pips adicionais necessários antes que o trailing stop avance. Deve ser positivo quando o trailing está habilitado.
| `Money Mode` | Seleciona entre dimensionamento `FixedVolume` e `RiskPercent`.
| `Money Value` | Interpretado como tamanho de lote com volume fixo, ou como percentual de risco com dimensionamento baseado em risco.
| `Order Volume` | Volume base usado quando `Money Mode` é `FixedVolume`.
| `Stochastic %K` | Comprimento do cálculo estocástico %K.
| `Stochastic %D` | Comprimento de suavização para a linha estocástica %D.
| `Stochastic Slow` | Fator de suavização final aplicado ao oscilador estocástico.
| `Level Up` | Limiar estocástico superior que habilita entradas vendidas.
| `Level Down` | Limiar estocástico inferior que habilita entradas compradas.
| `Signal Candles` | Período que impulsiona o loop principal de negociação.
| `Stochastic Candles` | Período que alimenta o oscilador estocástico.
| `AO Candles` | Período que alimenta o Awesome Oscillator.
| `AO Fast` / `AO Slow` | Períodos para as médias móveis internas do Awesome Oscillator.

## Notas de Implementação
- O cálculo do valor do pip emula a lógica do MetaTrader: quando o instrumento usa 3 ou 5 casas decimais, um pip equivale a dez passos de preço; caso contrário equivale a um passo de preço.
- O oscilador estocástico do StockSharp não expõe uma seleção dedicada de "campo de preço"; o port usa o cálculo padrão baseado em fechamento, mantendo os parâmetros de suavização configuráveis.
- O tratamento do trailing stop é implementado como uma verificação virtual nas máximas/mínimas das velas, replicando os ajustes de stop do lado servidor realizados no MetaTrader sem registrar ordens de stop explícitas.
- O código assina todos os períodos de velas necessários através de `GetWorkingSecurities`.
- Comentários em inglês documentam as decisões de fluxo de controle mais importantes.

## Dicas de Uso
- Alinhe o período de `Signal Candles` com o período em que planeja fazer backtesting ou negociar. Mantenha `Stochastic Candles` e `AO Candles` iguais aos padrões originais quando quiser replicar exatamente o especialista MQL5.
- Ao mudar para dimensionamento `RiskPercent`, certifique-se de que a distância de stop-loss seja diferente de zero; caso contrário a estratégia recorre a `Order Volume`.
- A configuração de trailing padrão reflete o EA original (trailing stop de 25 pips com passo de 5 pips). Defina `Trailing Stop` como zero se preferir um stop-loss estático.
