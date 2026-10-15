# Estratégia Kloss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Kloss combina uma média móvel ponderada (WMA), o Índice de Canal de Commodities (CCI) e o oscilador Stochastic. Todos os indicadores são avaliados em valores históricos deslocados, permitindo que os sinais sejam baseados no contexto de mercado passado. Uma posição comprada é aberta quando o CCI cai abaixo de um limiar negativo, a linha principal do Stochastic cai abaixo de um desvio do nível neutro 50, e o preço deslocado está acima do WMA deslocado. Uma posição vendida é aberta nas condições opostas. O fechamento inverso opcional sai de uma posição existente quando o sinal oposto aparece. O stop loss e o take profit são definidos em pontos a partir do preço de entrada.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: CCI deslocado abaixo de `-CciDiffer`, Stochastic deslocado abaixo de `50 - StochDiffer`, e preço deslocado acima do WMA deslocado.
  - **Vendido**: CCI deslocado acima de `CciDiffer`, Stochastic deslocado acima de `50 + StochDiffer`, e preço deslocado abaixo do WMA deslocado.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Sinal inverso se `RevClose` estiver habilitado ou níveis de stop loss / take profit.
- **Stops**: Stop loss e take profit absolutos em pontos.
- **Filtros**:
  - Os deslocamentos de indicadores e preços via `CommonShift` permitem a geração de sinais a partir de barras passadas.
