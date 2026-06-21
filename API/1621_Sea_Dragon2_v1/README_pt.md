# Sea Dragon 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sea Dragon 2 é uma estratégia de grade com hedge que abre posições em ambas as direções e adiciona novas ordens quando o preço se move por um passo definido pelo usuário. Os tamanhos das ordens seguem uma sequência predefinida e os níveis de take profit se adaptam dependendo do equilíbrio entre a exposição comprada e vendida.

## Detalhes

- **Ordens iniciais**: Abre tanto uma ordem de compra quanto uma de venda com o mesmo volume no início.
- **Adição de ordens**: Quando o mercado se move *Step* pontos a partir do preço da última ordem, um novo par de ordens é adicionado. O lado com maior exposição recebe a ordem maior de acordo com a sequência.
- **Sequência de volume**: 1,1,2,3,6,9,14,22,33,48,82,111,122,164,185 escalado por *Volume Scale*.
- **Take Profit**:
  - Quando os contadores comprado e vendido são iguais, cada lado usa *Take Profit*.
  - Se um lado domina, esse lado usa *Alt Take Profit* enquanto o outro mantém *Take Profit*.
- **Stop Loss**: Cada lado tem um stop colocado a *Max Stop* pontos de seu preço médio.
- **Fonte de dados**: A estratégia opera em velas concluídas do tipo *Candle Type*.
- **Comprado/Vendido**: Ambos, com hedge.
- **Saída**: As posições fecham quando o preço atinge os níveis de take profit ou stop.
