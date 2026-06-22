# Estratégia IBS RSI CCI v4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia IBS RSI CCI v4** é um sistema de trading contrário que combina três osciladores de momentum:

- **Internal Bar Strength (IBS)** – mede a posição relativa do fechamento dentro do intervalo máximo-mínimo da barra e é suavizado com uma média móvel configurável.
- **Relative Strength Index (RSI)** – captura o momentum do mercado em torno do nível neutro de 50.
- **Commodity Channel Index (CCI)** – avalia o desvio do preço de uma linha de base de média móvel.

Os três componentes são escalados e combinados em um oscilador composto. O sinal composto é restringido por um limite de passo configurável e filtrado através de um envelope de máximos/mínimos de estilo Donchian. Cruzamentos entre o sinal composto e sua linha média geram oportunidades de reversão.

## Lógica de trading
1. Assinar velas com o período selecionado (padrão: 4 horas).
2. Calcular o valor IBS para cada vela terminada e suavizá-lo com o tipo de média móvel escolhido.
3. Obter valores RSI e CCI usando seus respectivos comprimentos de lookback.
4. Construir o oscilador composto usando a ponderação original do script do MetaTrader:
   - Contribuição IBS × 700
   - Desvio RSI de 50 × 9
   - Valor CCI bruto × 1
5. Aplicar um limite de passo para evitar saltos repentinos no sinal composto.
6. Rastrear o máximo e mínimo contínuos do sinal composto e suavizar ambas as bordas para formar uma banda dinâmica. A linha média da banda é usada como "linha de base" (equivalente ao segundo buffer de indicador na versão MQL).
7. **Gestão de posição**
   - Fechar posições compradas quando o sinal composto está abaixo da linha de base na barra confirmada.
   - Fechar posições vendidas quando o sinal composto está acima da linha de base na barra confirmada.
   - Abrir posições compradas quando a barra confirmada anteriormente estava acima da linha de base e o sinal mais recente cruza para baixo através da linha de base (entrada contrária).
   - Abrir posições vendidas quando a barra confirmada anteriormente estava abaixo da linha de base e o sinal mais recente cruza para cima através da linha de base.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Série de velas usada para cálculos de indicadores. |
| `IbsPeriod` | Comprimento de lookback usado para suavizar o componente IBS. |
| `IbsAverageType` | Tipo de média móvel para suavização IBS (Simples, Exponencial, Suavizado, Ponderado Linear). |
| `RsiPeriod` | Comprimento de lookback RSI. |
| `CciPeriod` | Comprimento de lookback CCI. |
| `RangePeriod` | Tamanho da janela para a banda contínua de máximos/mínimos aplicada ao sinal composto. |
| `SmoothPeriod` | Comprimento da média móvel usado para suavizar as bordas da banda de máximos/mínimos. |
| `RangeAverageType` | Tipo de média móvel para suavização da banda (Simples, Exponencial, Suavizado, Ponderado Linear). |
| `StepThreshold` | Ajuste máximo aplicado quando o sinal composto salta bruscamente entre barras. |
| `SignalBar` | Número de velas já fechadas usadas para confirmação (padrão 1 replica o comportamento original). |
| `EnableLongOpen` | Permitir abertura de novas posições compradas. |
| `EnableShortOpen` | Permitir abertura de novas posições vendidas. |
| `EnableLongClose` | Permitir fechamento de posições compradas existentes. |
| `EnableShortClose` | Permitir fechamento de posições vendidas existentes. |
| `OrderVolume` | Volume base da ordem de mercado enviada nas entradas. |

## Notas de implementação
- A restrição de passo replica a lógica de limitação de buffer do indicador MQL. Um `StepThreshold` maior permite saltos maiores no oscilador composto.
- Apenas as quatro famílias de médias móveis mais comuns são suportadas para suavização IBS e de envelope, porque a biblioteca padrão do StockSharp não inclui os filtros personalizados do arquivo de recursos do MetaTrader.
- A estratégia usa `SignalBar` para atrasar os sinais em uma vela completamente fechada, correspondendo ao comportamento do consultor especialista original.
- Por padrão, a estratégia é totalmente contrária: os sinais são gerados contra a direção do cruzamento mais recente. Alterne os booleanos de entrada/saída para limitar a estratégia a uma única direção, se desejado.

## Uso
1. Configure o `CandleType` para corresponder ao período do instrumento alvo.
2. Ajuste os comprimentos dos indicadores e o limite de passo para se adequar à volatilidade do instrumento.
3. Ative ou desative as entradas e saídas compradas/vendidas de acordo com sua preferência de trading.
4. Configure o parâmetro `OrderVolume` para controlar o tamanho da ordem e inicie a estratégia. `StartProtection()` está ativado por padrão e pode ser personalizado se regras de risco adicionais forem necessárias.
5. Revise o painel de gráficos (se disponível) para monitorar os preços das velas, o oscilador composto e as operações registradas.

## Diferenças da versão do MetaTrader
- Os parâmetros de gestão de dinheiro e desvio de ordem do EA original são substituídos pelo parâmetro `OrderVolume` do StockSharp e ordens de mercado de alto nível.
- A conversão do StockSharp mantém as ponderações originais do indicador e a lógica de reversão, mas concentra-se nos filtros de média móvel mais comumente usados.
- Stops protetores não estão pré-configurados; combine a estratégia com módulos de risco do StockSharp se stops fixos ou take-profits forem necessários.
