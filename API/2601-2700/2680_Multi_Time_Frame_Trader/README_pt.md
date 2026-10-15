# Estratégia de Operador em Múltiplos Períodos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia recria a lógica MQL original do "Multi Time Frame Trader" com as APIs de alto nível do StockSharp. Combina três
canais de regressão polinomial (M1, M5 e H1) e opera somente quando os períodos inferiores testam os extremos do canal na
direção sugerida pela inclinação horária.

O sistema recalcula continuamente as bandas superior, intermediária e inferior do canal de regressão em cada vela concluída. Quando
a banda superior horária diminui, o viés é de baixa; quando sobe, o viés é de alta. As entradas são acionadas assim que as velas
M5 e M1 alcançam a banda correspondente e o filtro direcional concorda.

## Fluxo de trabalho principal

- **Assinaturas**: a estratégia escuta velas de 1 minuto, 5 minutos e 1 hora simultaneamente.
- **Canal de regressão**: cada assinatura constrói uma linha de regressão polinomial (grau 1-3) sobre `Bars` pontos e a desloca
  `StdMultiplier` desvios padrão para obter bandas de resistência e suporte.
- **Estimação de inclinação**: a inclinação do canal é derivada da diferença entre a banda superior atual e a banda superior há `Bars`
  velas atrás, espelhando o comportamento do indicador `i-Regr`.
- **Filtro direcional**: a inclinação H1 define se apenas posições vendidas (inclinação negativa) ou compradas (inclinação positiva)
  são permitidas.

## Lógica de entrada

### Operações vendidas

1. A inclinação horária é negativa.
2. O máximo da última vela de 5 minutos toca ou rompe a resistência de regressão de 5 minutos.
3. O máximo da última vela de 1 minuto toca ou rompe a resistência de 1 minuto.
4. Não há posição vendida existente aberta (`Position >= 0`).
5. Uma ordem de venda a mercado é enviada, o stop-loss é colocado a meia largura do canal acima da entrada e o alvo é igual à
   linha média do M5.

### Operações compradas

1. A inclinação horária é positiva.
2. O mínimo da última vela de 5 minutos toca ou rompe o suporte de regressão de 5 minutos.
3. O mínimo da última vela de 1 minuto toca ou rompe o suporte de 1 minuto.
4. Não há posição comprada existente aberta (`Position <= 0`).
5. Uma ordem de compra a mercado é enviada, o stop-loss é colocado a meia largura do canal abaixo da entrada e o alvo é igual à
   linha média do M5.

## Regras de saída

- Stops e alvos são armazenados internamente e avaliados em cada vela M1 concluída. Se o intervalo da vela cruzar o nível de stop
  armazenado, a posição é fechada imediatamente.
- Se o alvo de lucro for alcançado antes do stop, a posição também é fechada.
- O fechamento redefine os níveis rastreados para que um novo sinal possa ser avaliado sem demora.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `Degree` | 1 | Grau polinomial do canal de regressão (1=linear, 2=parabólico, 3=cúbico). |
| `StdMultiplier` | 2.0 | Multiplicador do desvio padrão que define a largura da banda. |
| `Bars` | 250 | Número de velas usadas para o ajuste de regressão e o retrocesso de inclinação. |
| `Shift` | 0 | Deslocamento horizontal do ponto de avaliação de regressão (limitado entre 0 e `Bars - 1`). |
| `UseTrading` | true | Desabilita toda a geração de ordens quando definido como `false`, enquanto o canal continua a ser atualizado. |

## Notas adicionais

- A estratégia armazena os níveis de stop e alvo localmente porque as ordens a mercado do StockSharp não anexam automaticamente
  níveis SL/TP.
- Funciona em qualquer instrumento que suporte velas de minutos e horas; no entanto, a lógica original foi desenvolvida para pares
  de forex.
- Ajuste `Bars` para corresponder à volatilidade do instrumento negociado. Um valor menor reage mais rápido, um valor maior produz
  canais mais suaves.
- Defina `Degree` como 1 para um canal de regressão linear direto (o mais próximo da versão linear clássica), ou use graus mais
  altos para emular os modos polinomiais do indicador MQL.
