# Estratégia de IMF AH HM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo

A estratégia AH HM MFI negocia padrões de velas de martelo e homem enforcado que são confirmados pelo Money Flow Index (MFI). Quando um martelo de alta aparece numa tendência descendente de curto prazo e a IMF permanece abaixo de um limite de sobrevenda, a estratégia abre uma posição longa. Quando um homem pendente de baixa se forma em uma tendência de alta enquanto a IMF está acima do limite de sobrecompra, ele abre uma posição curta. As saídas de proteção são acionadas quando a IMF ultrapassa limites superiores ou inferiores predefinidos.

## Lógica principal

1. Assine as velas de período de tempo configuradas e calcule dois indicadores:
   - **Índice de Fluxo de Dinheiro** com período configurável (padrão: 47).
   - **Média Móvel Simples** dos preços de fechamento para aproximar o filtro de tendência da estratégia MQL original (comprimento padrão: 5).
2. Detecte padrões de **martelo** e **homem enforcado**:
   - Corpo da vela localizado no terço superior da faixa.
   - Sombra inferior longa em relação ao corpo real.
   - Gap na direção da tendência em comparação com a vela anterior.
   - Confirmação da tendência usando o ponto médio da vela anterior versus a média móvel.
3. Confirme as entradas com limites de IMF:
   - Insira longo se um martelo for detectado e o MFI estiver no nível de sobrevenda configurado ou abaixo dele (padrão: 40).
   - Digite short se um homem enforcado for detectado e a MFI estiver no nível de sobrecompra configurado ou acima dele (padrão: 60).
4. Gerenciar saídas usando cruzamentos de IMF:
   - Fechar posições curtas quando a IMF cruzar para cima acima dos níveis de saída inferior ou superior (padrões: 30 e 70).
   - Fechar posições longas quando a IMF cruzar para cima acima do nível de saída superior ou para baixo abaixo do nível de saída inferior.
5. Inicie o módulo integrado de proteção contra riscos para lidar com paradas de emergência.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Tipo de dados de vela e período de tempo usado para detecção de padrão. | Período de 30 minutos |
| `MfiPeriod` | Período retrospectivo para o cálculo da IFM. | 47 |
| `MaPeriod` | Comprimento do SMA aplicado aos preços de fechamento para confirmação da tendência. | 5 |
| `HammerEntryThreshold` | Valor máximo de MFI permitido antes de entrar em um sinal de martelo. | 40 |
| `HangingEntryThreshold` | Valor MFI mínimo exigido antes de entrar em um sinal de homem suspenso. | 60 |
| `MfiUpperExitLevel` | Limite superior das IFM; cruzar acima dele fecha qualquer posição aberta. | 70 |
| `MfiLowerExitLevel` | Limite inferior das IMFs; cruzar abaixo fecha posições longas, enquanto cruzar acima fecha posições curtas. | 30 |

## Notas

- A estratégia avalia apenas velas finalizadas para evitar agir com base em informações incompletas.
- A detecção do martelo e do homem enforcado é conservadora: são necessárias uma longa sombra inferior e um corpo localizado próximo à altura da vela.
- A média móvel substitui o filtro MetaTrader 5 `CloseAvg` do consultor especialista original, garantindo que as entradas estejam alinhadas com a tendência mais ampla.
