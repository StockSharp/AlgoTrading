# Estratégia TCPivot Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera rompimentos através da linha de pivô diária. Calcula os níveis de pivô clássicos dos traders de floor a partir da máxima, mínima e fechamento do dia anterior. Uma posição comprada é aberta quando o preço de fechamento cruza acima do pivô. Uma posição vendida é aberta quando o preço de fechamento cruza abaixo do pivô.

Após a entrada, o sistema utiliza um dos níveis de suporte ou resistência tanto como alvo de lucro quanto de stop loss. O nível é selecionado pelo parâmetro **Target Level**:

- **1** – usa `Support1`/`Resistance1`.
- **2** – usa `Support2`/`Resistance2`.
- **3** – usa `Support3`/`Resistance3`.

Se **Intraday Only** estiver ativado, todas as posições abertas são fechadas às 23:00 horário da plataforma.

## Detalhes

- **Critérios de entrada**
  - **Comprado**: fechamento anterior ≤ pivô e fechamento atual > pivô.
  - **Vendido**: fechamento anterior ≥ pivô e fechamento atual < pivô.
- **Critérios de saída**
  - **Comprado**: fechamento ≥ nível de resistência selecionado ou fechamento ≤ nível de suporte selecionado.
  - **Vendido**: fechamento ≤ nível de suporte selecionado ou fechamento ≥ nível de resistência selecionado.
  - Se *Intraday Only* for verdadeiro, todas as posições são fechadas às 23:00.
- **Indicadores**: apenas cálculo clássico de pivô.
- **Período**: configurável; velas de 5 minutos por padrão.
- **Stops**: stop-loss e take-profit baseados no nível de pivô escolhido.
