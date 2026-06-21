# Estratégia de Bill Williams Wise Man 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa o segundo padrão "sábio" do sistema de trading de Bill Williams.
Ela analisa o histograma do Awesome Oscillator (AO) para identificar mudanças de momentum:

- **Compra** quando o AO está acima de zero e forma um pico seguido de três barras consecutivamente mais baixas.
- **Venda** quando o AO está abaixo de zero e forma um vale seguido de três barras consecutivamente mais altas.

Sempre que um sinal aparece, a estratégia fecha a posição oposta e abre uma nova na
direção do sinal. Por padrão são usadas velas de quatro horas, mas o período pode ser alterado
através de um parâmetro.

Não há lógica de stop-loss ou take-profit; as posições são revertidas apenas quando surge um padrão
oposto. A estratégia também plota velas, o indicador AO e as operações executadas em um gráfico
para análise visual.
