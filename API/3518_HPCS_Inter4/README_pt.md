# Estratégia HPCS Inter4 (3518)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia transporta o consultor especialista MetaTrader "_HPCS_IntFourth_MT4_EA_V01_WE" para o StockSharp API de alto nível. O script original abre imediatamente uma posição longa, aplica níveis protetores de stop-loss e take-profit medidos em MetaTrader pips e fecha a negociação à força após um curto período de manutenção. A versão C# reproduz o mesmo comportamento combinando o gerenciador de proteção integrado com um temporizador de um segundo que monitora o tempo decorrido desde a entrada.

## Lógica de negociação

1. **Inicialização**
   - Quando a estratégia é iniciada, ela calcula o tamanho do pip MetaTrader a partir do título `PriceStep` e da precisão decimal (símbolos de 5 e 3 dígitos usam um multiplicador de 10x).
   - O auxiliar `StartProtection` de alto nível é configurado com as distâncias de take-profit e stop-loss solicitadas. A distância de stop-loss inclui o buffer extra que o EA original aplica usando `OrderModify`.
   - O volume é fixo e vem do parâmetro `OrderVolume`.

2. **Entrada**
   - Uma ordem de compra no mercado único é enviada imediatamente após o lançamento da estratégia. Nenhuma outra entrada é colocada.
   - Assim que o primeiro preenchimento é relatado, a estratégia armazena o tempo de execução.

3. **Sair**
   - Um temporizador verifica a posição aberta a cada segundo.
   - Quando o período de manutenção atinge `CloseDelaySeconds`, a estratégia fecha a posição longa com uma ordem de venda a mercado se a exposição ainda for positiva.
   - As ordens protetoras de stop-loss e take-profit são mantidas automaticamente pelo gestor de proteção usando saídas de mercado.

A lógica negocia apenas na direção longa, refletindo o comportamento do script MetaTrader.

## Parâmetros

| Nome | Descrição | Padrão | Otimizável |
| --- | --- | --- | --- |
| `OrderVolume` | Volume fixo utilizado no envio da ordem inicial de compra a mercado. | `1` | Não |
| `StopLossPips` | Distância base de MetaTrader pip aplicada ao stop-loss inicial. | `10` | Não |
| `ExtraStopPips` | Buffer pip adicional MetaTrader subtraído da parada após a entrada. | `10` | Não |
| `TakeProfitPips` | Distância de MetaTrader pip da meta de lucro. | `10` | Não |
| `CloseDelaySeconds` | Tempo em segundos antes que a posição seja fechada à força. `0` desativa a saída do temporizador. | `30` | Não |

## Notas de implementação

- O auxiliar de tamanho de pip multiplica o `PriceStep` relatado por 10 para instrumentos de 3 e 5 decimais para que os valores dos parâmetros mantenham a mesma escala de MetaTrader.
- `StartProtection` usa distâncias `UnitTypes.Price` para que as ordens de proteção operem com saídas de mercado, exatamente como o EA fez com `OrderClose`.
- `OnNewMyTrade` registra a primeira negociação de compra preenchida para iniciar a contagem regressiva do período de manutenção e redefine o estado quando a posição é totalmente fechada.
- O cronômetro é executado em intervalos de um segundo para replicar a verificação de tempo `OnTick` original, permanecendo insensível à inatividade do mercado.
- Todos os comentários do código são escritos em inglês para cumprir as diretrizes do repositório.
