# Estratégia Simples OzFx
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Conversão do consultor especialista MetaTrader 4 **OzFx** (pasta `MQL/7994`) para o StockSharp API de alto nível.
- Usa o oscilador acelerador/desacelerador (AC) junto com a linha %K do oscilador estocástico para detectar reversões de momento em torno da linha zero.
- Replica o comportamento do especialista de empilhar cinco ordens de mercado com lucros escalonados e proteção de equilíbrio após a primeira meta ser atingida.

## Lógica de negociação
1. Construa o Awesome Oscillator (5/34) e subtraia seus 5 períodos SMA para obter o valor do Accelerator Oscillator da vela concluída anterior e atual.
2. Assine o oscilador estocástico (%K length = `StochasticLength`, suavização 3/3) e leia a linha principal no fechamento da vela.
3. **Configuração longa** requer:
   - `%K` acima do nível médio configurado (padrão 50).
   - Valor AC atual positivo e superior ao anterior.
   - Valor AC anterior ainda abaixo de zero (momentum cruza a linha de base).
4. **Configuração curta** reflete as regras na direção oposta.
5. Quando um sinal aparece numa nova barra, a estratégia abre cinco ordens de mercado iguais:
   - As camadas 1 a 4 recebem lucros espaçados por `TakeProfitPips` múltiplos.
   - A camada 5 não tem meta de lucro e continua acompanhando o movimento.
6. Se a configuração oposta aparecer enquanto uma pilha estiver aberta, as ordens restantes serão fechadas no mercado, mantendo a estratégia estável antes de novas entradas.

## Gerenciamento de posição
- Cada camada compartilha a mesma distância de stop loss definida por `StopLossPips`.
- Após a execução do primeiro take-profit, as ordens restantes estreitam seus stops até o preço de equilíbrio (entrada), correspondendo à lógica "modok" original.
- As saídas de proteção são executadas quando os extremos da vela perfuram o stop armazenado ou os níveis alvo; ordens pendentes do lado da corretora não são usadas.
- A estratégia permite apenas uma direção por vez e aguarda o fechamento de todas as ordens antes de redefinir os sinalizadores de bloqueio de entrada.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `OrderVolume` | Tamanho do lote para cada uma das cinco ordens de mercado. | `0.1` |
| `StopLossPips` | Distância entre entrada e stop loss, expressa em pips. | `100` |
| `TakeProfitPips` | Incremento entre níveis consecutivos de take-profit (camadas 1-4). | `50` |
| `StochasticLevel` | Limite aplicado ao valor %K estocástico. | `50` |
| `StochasticLength` | Período de lookback do cálculo %K estocástico. | `5` |
| `CandleType` | Série de velas de origem usada pela estratégia (o padrão é velas de 4 horas). | `4h time-frame` |

## Notas de implementação
- Os sinais são avaliados apenas em velas finalizadas para permanecerem consistentes com o especialista MT4 que trabalha em novas barras.
- A conversão de pip se adapta automaticamente a símbolos forex de 3/5 dígitos, multiplicando o preço mínimo por 10 quando necessário.
- As entradas e saídas escalonadas são tratadas na memória por meio de objetos em camadas para que a estratégia possa fechar adequadamente partes da posição.
- Todos os comentários dentro do código C# são escritos em inglês, conforme exigido pelas diretrizes do repositório.
