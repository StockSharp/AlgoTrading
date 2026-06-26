# Estratégia de Three Breaky
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de Three Breaky** é uma conversão completa do expert advisor MetaTrader 4 `ThreeBreaky_v1.mq4`. A versão StockSharp mantém o trio original de subsistemas de rompimento, traduz sua lógica baseada em velas para a API de alto nível e adiciona controle claro de posições para cada módulo. A estratégia trabalha em um único período configurável e pode habilitar ou desabilitar qualquer subsistema sem afetar os outros.

## Módulos de negociação

1. **Sistema 1 – Rompimento de expansão ATR**
   - Usa apenas a vela anterior.
   - Vai comprado quando a vela anterior é de alta e seu intervalo máximo-mínimo excede quatro vezes o average true range de 72 períodos.
   - Vai vendido quando a vela anterior é de baixa e a mesma condição de intervalo é satisfeita.

2. **Sistema 2 – Flip de nuvem Ichimoku**
   - Observa os limites da nuvem (Senkou Span A e Senkou Span B) com períodos padrão 9/26/52.
   - Um sinal comprado é acionado quando duas velas atrás fechou abaixo de ambos os spans e o último fechou acima de ambos (um flip de alta através da nuvem).
   - Um sinal vendido é acionado quando duas velas atrás fechou acima de ambos os spans e o último fechou abaixo de ambos.

3. **Sistema 3 – Rompimento de corpo excepcional**
   - Rastreia o tamanho do corpo das últimas 20 velas completadas.
   - Uma configuração comprada exige que a vela anterior seja de alta e seu corpo seja mais de três vezes o corpo máximo observado nessa história de 20 velas.
   - Uma configuração vendida espelha a condição para corpos de baixa.

Cada subsistema negocia uma posição virtual dedicada. Os carimbos de tempo das ordens são armazenados para garantir que um módulo possa abrir no máximo uma operação por vela, assim como a lógica original `buyTag` e `sellTag`.

## Lógica de saída

- **Reversão de SAR Parabólico**: Todas as posições abertas compartilham uma saída de SAR Parabólico (0.005/0.2). Quando o preço cruza o SAR entre as duas últimas velas, a posição afetada é fechada.
- **Gestão de risco**: Distâncias opcionais de stop-loss e take-profit (em pips) são avaliadas em cada vela completada. Se os limiares configurados forem violados, a posição relevante é fechada imediatamente.

## Indicadores utilizados

- Average True Range (período 72) para a linha de base de volatilidade média.
- Ichimoku Kinko Hyo (9, 26, 52) para o filtro de flip de nuvem.
- SAR Parabólico (aceleração 0.005, máximo 0.2) para saídas e lógica de trailing.
- Buffer de tamanho de corpo rotativo (20 velas) para reproduzir a comparação de corpo máximo MQL.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `UseSystem1` | Habilita o módulo de rompimento de expansão ATR. |
| `UseSystem2` | Habilita o módulo de flip de nuvem Ichimoku. |
| `UseSystem3` | Habilita o módulo de rompimento de corpo grande. |
| `OrderVolume` | Volume usado para cada ordem a mercado gerada por qualquer módulo. |
| `StopLossPips` | Distância de stop protetor em pips. Definir como zero para desativar. |
| `TakeProfitPips` | Distância de take-profit em pips. Definir como zero para desativar. |
| `CandleType` | Período para as velas de trabalho (padrão 1 hora). |

## Resumo do fluxo de trabalho

1. Assinar a série de velas configurada e processar apenas velas finalizadas.
2. Atualizar os indicadores ATR, Ichimoku e SAR Parabólico junto com o histórico de corpo rotativo.
3. Fechar posições que atingirem stops, alvos ou reversões de SAR Parabólico.
4. Se a negociação for permitida, avaliar cada subsistema independentemente e emitir ordens a mercado quando todas as condições respectivas forem atendidas.
5. Armazenar as últimas saídas dos indicadores para que a próxima vela possa acessar os mesmos valores históricos da implementação MQL original.

## Notas

- A estratégia assume um valor de pip baseado no passo de preço do instrumento; cotações FX de cinco e três dígitos são normalizadas para tamanhos de pip de quatro e dois decimais, respectivamente.
- Os subsistemas podem rodar simultaneamente. Cada um mantém seu próprio preço de entrada, direção de posição e últimos carimbos de tempo de sinal para espelhar a separação `MagicNumber+N` do EA fonte.
- A implementação StockSharp retém o padrão de execução "uma vez por barra" usando tempos de abertura de velas para bloquear ordens duplicadas dentro de uma única barra.
