# Estratégia RAVIiAO (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia RAVIiAO** reproduz o MetaTrader 4 consultor especialista "RAVIiAO" dentro do StockSharp API de alto nível. O sistema
espera o fechamento de uma nova vela, avalia a inclinação do oscilador RAVI junto com a aceleração/desaceleração (AC) de Bill Williams
oscilador e abre uma posição imediatamente no mercado quando ambos os indicadores concordam com a direção da tendência. O porto mantém o
conjunto de parâmetros original – períodos de média móvel, limite, distâncias de stop-loss/take-profit e volume de pedidos – permitindo aos traders
para replicar o comportamento legado sem ajustes manuais.

## Fluxo de trabalho principal
1. **Assinatura de velas** – a estratégia assina um período de tempo configurável (velas de 30 minutos por padrão).
2. **Atualizações do indicador** – em cada vela finalizada ele atualiza duas médias móveis simples para construir o oscilador RAVI e feeds
a mesma vela em um par Awesome Oscillator + suavização de 5 períodos para obter o valor AC.
3. **Preparação do sinal** – a última vela finalizada é armazenada como "barra 1" enquanto o valor anterior se torna "barra 2", correspondendo ao
Chamadas `iCustom(...,1)` e `iCustom(...,2)` de MetaTrader.
4. **Decisão de entrada** – uma posição longa é aberta quando AC e RAVI aumentam acima de seus valores anteriores e confirmam uma
ambiente otimista (`AC[1] > AC[2] > 0` e `RAVI[1] > RAVI[2] > Threshold`). As negociações curtas usam as condições espelhadas.
5. **Gerenciamento de risco** – assim que uma ordem é executada, a estratégia registra níveis estáticos de stop-loss e take-profit expressos em
pontos do instrumento (ou seja, `StopLossPoints * PriceStep`). As velas são monitoradas quanto a violações intrabar usando seus preços altos/baixos.
6. **Redefinição de estado** – quando um nível de proteção é atingido, a posição é fechada com uma ordem de mercado e os buffers internos são redefinidos
para a próxima oportunidade.

## Regras de negociação
- **Entradas longas**
  - Valor AC anterior acima do valor AC anterior e ambos maiores que zero.
  - Leitura anterior de RAVI acima do limite e do valor RAVI anterior.
  - Nenhuma posição ativa no momento do sinal.
- **Entradas curtas**
  - Valor AC anterior abaixo do valor AC anterior e ambos abaixo de zero.
  - Leitura anterior do RAVI abaixo do limite negativo e abaixo do valor anterior do RAVI.
  - Nenhuma posição ativa quando o sinal dispara.
- **Saídas de posição**
  - Os níveis estáticos de stop-loss e take-profit são expressos em pontos brutos, convertidos em compensações de preço por meio do instrumento `PriceStep`.
  - As violações são detectadas com extremos de vela (baixo para paradas longas, alto para paradas curtas, etc.) e fechadas imediatamente via mercado
ordens para emular as ordens de proteção de MetaTrader.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Período usado para assinatura de velas (padrão 30 minutos). |
| `FastLength` | Comprimento médio móvel rápido usado no oscilador RAVI. |
| `SlowLength` | Comprimento médio de movimento lento usado no oscilador RAVI. |
| `Threshold` | Porcentagem absoluta mínima de RAVI para validar uma continuação de tendência. |
| `StopLossPoints` | Distância de stop-loss em pontos do instrumento (multiplicada por `PriceStep`). |
| `TakeProfitPoints` | Distância de lucro em pontos de instrumento. |
| `TradeVolume` | Volume de ordens de mercado para cada entrada. |

## Notas de conversão
- A porta StockSharp armazena os dois valores mais recentes do indicador para que a decisão na vela *n* reutilize o `AC[1]` e
`RAVI[1]` valores de MetaTrader (ou seja, resultados da barra anterior), preservando o estilo de execução "nova barra" de EA.
- AC é reconstruído através da diferença entre o Awesome Oscillator e sua média móvel simples de 5 períodos, correspondendo ao MT4
cadeia de cálculo.
- Stops e metas são avaliados em relação aos extremos das velas, em vez de colocar ordens de proteção pendentes; isso reflete o efeito
do tratamento SL/TP integrado de MetaTrader, mantendo a implementação idiomática para StockSharp.

## Dicas de uso
- Certifique-se de que o instrumento selecionado exponha um `PriceStep` correto; caso contrário, as distâncias de proteção não corresponderão à versão MT4.
- Otimize os parâmetros `Threshold`, `FastLength` e `SlowLength` ao adaptar a estratégia a mercados com diferentes
características de volatilidade.
- Combine a estratégia com proteções em nível de portfólio ou conector StockSharp para segurança adicional durante negociações ao vivo.
