# Estratégia NextBar Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera rompimentos de momentum que ocorrem quando a barra completada mais recente fecha bem longe de uma barra de referência mais antiga. Foi inspirada pelo consultor especialista MetaTrader "Nextbar" e mantém as características originais de gestão monetária, como stops baseados em pips, lógica de trailing e tempo de vida limitado da posição.

A configuração padrão mira em gráficos de FX ou futuros de índices de movimento rápido no período de 15 minutos, mas a lógica funciona em qualquer símbolo que forneça velas regulares. Cada ordem é enviada a mercado usando o tamanho de posição configurado.

## Lógica de trading

- **Detecção de sinal**
  - Quando uma nova barra termina, o algoritmo compara o fechamento da barra anterior com o fechamento que ocorreu `SignalBar` barras atrás.
  - Se o fechamento anterior for maior do que o fechamento distante em mais de `MinDistancePips`, uma configuração comprada é gerada.
  - Se o fechamento anterior for menor do que o fechamento distante em mais de `MinDistancePips`, aparece uma configuração vendida.
  - O interruptor `ReverseSignals` inverte a direção de cada configuração para se adequar a fluxos de trabalho contrários.
- **Tratamento de ordens**
  - As ordens são ignoradas enquanto uma posição está aberta. A estratégia mantém apenas uma única posição de cada vez, assim como o consultor especialista original.
  - Cada preenchimento armazena o preço de entrada e pré-calcula os níveis de stop-loss e take-profit em unidades de preço. Os valores baseados em pips são convertidos usando o passo de preço da segurança (instrumentos de 5 dígitos usam automaticamente um multiplicador de 10× para corresponder ao tamanho de pip do MetaTrader).

## Regras de saída

- **Stop loss / take profit** – Ambos os níveis são opcionais. Um valor de zero desabilita a proteção correspondente. A estratégia monitora as máximas e mínimas das velas para acionar saídas quando os níveis são cruzados.
- **Trailing stop** – Quando habilitado (`TrailingStopPips` > 0), o stop é movido mais perto do preço atual assim que o lucro excede `TrailingStopPips + TrailingStepPips`. A distância do preço ao stop nunca diminui, garantindo um comportamento de trailing monótono.
- **Tempo de vida da posição** – Depois de permanecer no mercado por `LifetimeBars` velas completadas, a posição é fechada na próxima abertura de barra independentemente do lucro. Isso reproduz o mecanismo original de "expirar após N barras".

## Parâmetros

- `CandleType` – Período usado para avaliação de sinais. Padrão: velas de 15 minutos.
- `OrderVolume` – Quantidade enviada com cada ordem a mercado.
- `StopLossPips` – Distância do preço de entrada ao stop protetor, expressa em pips.
- `TakeProfitPips` – Distância do preço de entrada ao objetivo de lucro, expressa em pips.
- `TrailingStopPips` – Distância mantida pelo trailing stop. Definir como zero para desabilitar a lógica de trailing.
- `TrailingStepPips` – Lucro adicional necessário antes que o trailing stop avance novamente. Ignorado quando o trailing está desabilitado.
- `SignalBar` – Número de barras entre os fechamentos de comparação. Deve ser pelo menos dois para evitar referenciar a barra atual.
- `MinDistancePips` – Distância mínima em pips entre os fechamentos comparados antes que um sinal seja aceito.
- `LifetimeBars` – Número máximo de velas completadas que uma posição pode permanecer aberta. Definir como zero para desabilitar o temporizador.
- `ReverseSignals` – Inverte os sinais comprado/vendido quando habilitado.

## Notas de implementação

- A estratégia depende de uma breve lista rolante de fechamentos anteriores em vez de estruturas históricas pesadas, o que mantém o cálculo do sinal leve.
- Os pips são convertidos em unidades de preço usando o passo de preço da segurança. Instrumentos cotados com 3 ou 5 casas decimais são automaticamente mapeados para a definição tradicional de pip.
- Todos os controles de risco são aplicados em velas completadas. Se precisar de proteção intra-barra, combine a estratégia com ordens stop nativas do exchange através da configuração da plataforma.
- Nenhum teste automatizado é fornecido com esta amostra. Valide-a em dados históricos antes de usá-la em produção.
