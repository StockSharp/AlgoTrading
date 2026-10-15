# Estratégia de ruptura de piso TCPivotStop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de Breakout Floor TCPivotStop** é uma versão direta do consultor especialista MetaTrader `gpfTCPivotStop`. A lógica gira em torno
cálculos clássicos de pivô de piso realizados no dia de negociação anterior. No início de cada nova sessão diária a estratégia:

1. Agrega a máxima, a mínima e o fechamento do dia anterior para calcular o ponto de articulação mais os três primeiros níveis de suporte e resistência.
2. Verifica se a última barra horária concluída cruzou o pivô de cima ou de baixo.
3. Abre uma ordem de mercado na direção do cruzamento, ao mesmo tempo em que atribui níveis de stop-loss e take-profit que refletem o
comportamento original do perito.

Apenas uma posição pode estar ativa por vez. O gerenciamento de sessão opcional permite nivelar a exposição quando um novo dia começa.

## Regras de negociação

- **Prazo** – Projetado para velas de 1 hora (configurável).
- **Cálculo dinâmico** – usa a máxima, a mínima e o fechamento do dia anterior para calcular `Pivot`, `R1`, `R2`, `R3`, `S1`, `S2`, `S3`.
- **Condições de entrada**
  - Digite *short* quando a última barra concluída fechou abaixo do pivô enquanto a barra anterior fechou acima dele.
  - Digite *long* quando a última barra concluída fechou acima do pivô enquanto a barra anterior fechou abaixo dele.
- **Dimensionamento de posição** – Tamanho de lote fixo definido pelo parâmetro `OrderVolume`.
- **Condições de saída**
  - Os preços de stop-loss e take-profit são mapeados para os níveis de pivô clássicos.
  - Se a sinalização `CloseAtSessionEnd` estiver habilitada, a estratégia liquida as negociações abertas antes do início da próxima sessão.
  - Os níveis de proteção são monitorados nas máximas/mínimas das velas e executados com ordens de mercado quando tocados.

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `OrderVolume` | Tamanho da negociação para entradas no mercado. | `0.1` |
| `TakeProfitTarget` | Escolhe qual nível dinâmico atua como meta de lucro (`1` = mais próximo, `3` = mais distante). | `1` |
| `CloseAtSessionEnd` | Feche qualquer posição aberta assim que uma nova sessão diária começar. | `false` |
| `CandleType` | Período usado para todos os cálculos (de hora em hora por padrão). | `H1` |

## Notas

- A estratégia executa ordens apenas uma vez por dia quando um novo conjunto dinâmico está disponível, assim como a fonte EA que é acionada no
primeiro tick da sessão diária.
- A versão MetaTrader recalculou os tamanhos dos lotes usando o histórico de margem da conta. Esta porta mantém o dimensionamento da posição fixo e
delega a gestão do dinheiro a outros componentes, se necessário.
- As ordens de proteção são emuladas monitorando os extremos das velas e enviando ordens de mercado assim que um limite for ultrapassado.

## Arquivos

- `CS/TcpFloorPivotBreakoutStrategy.cs` – implementação em C# da lógica de negociação.
- `README.md` – Documentação em inglês (este arquivo).
- `README_zh.md` – Tradução simplificada para chinês.
- `README_ru.md` – Tradução russa.
