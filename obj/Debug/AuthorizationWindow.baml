   M S B A M L   `   `   `  ����L  HVeterinary_Clinic, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null5clr-namespace:Veterinary_ClinicVeterinary_Clinic  5   	   +    %Veterinary_Clinic.AuthorizationWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   1localclr-namespace:Veterinary_Clinic     5   	     � Title$F  @Авторизация - Ветеринарная Клиника��5   	   $	��750��5	   	   $
��1200��5
   	    � WindowStartupLocation$ CenterScreen=�5   	    � WindowStyle$
 None=�5   	    � AllowsTransparency . 
������  � 
ResizeMode$ CanResizeWithGrip=�5   	    � 	Resources 5         PrimaryColor  SecondaryColor  BackgroundColor  SurfaceColor  	TextColor  AccentColor  PrimaryBrush �� Color  SecondaryBrush  BackgroundBrush 	 SurfaceBrush 
 	TextBrush  AccentBrush  ModernButton �� 
TargetType �� Property	 �� Value
 �� 
TargetType �� CornerRadius �� Triggers P� Property P� Value N� EnterActions �� TargetProperty	 �� To g� Duration N� ExitActions  ModernTextBox ��Name  ModernPasswordBox��%�	  &	        &	      &	 >     &	 ]     &	 |     &	 �     &	 �     7   &	 �     7  &	 �     7  &		     7  &	
     7  &	 2    7  &	 J    7  7  7  7   &	 �    7 	 7 
 7  7  &	 �    7 	 7 
 7  7  ��5   
   #4ECDC4��6.   ��5   
   #A8E6CF��60   ��5   
   #F7F9FB��61   ��5   
   #E8F4F2��6.   ��5   
   #2D3E50��6+   ��5   
   #FF8C42��6-   �� 5   
   8   6/   �� 5   
   8   61   �� 5   
   8   62   �� 5   
   8   6/   �� 5   
   8   6,   �� 5   
   8   6.   �� 5"   
   " ��6%   .�5#      ��  � ��8	   6+   �� 5$       � ��
	 �����6+   �� 5%       � ��$	 0q�60   �� 5&       � ��$	 20,10q�6(   �� 5'       �@�Cursor$
	 Hand��6'   �� 5(       � ��	 5)      �� 5*      "
 ��6&   .R�5+      �� #��z��6!   $ 8��5,   !   #��z��5-   !   #��z��5.   !   ._�5/      �� $��Center=�6/   $��Center=�50   -   51      52      54       55      P� 56       �@J�IsMouseOver . . �57      ��  � ��8	  63    58      �� 59      .��5:      �� .��5;   "   �� $1 +(Button.Background).(SolidColorBrush.Color)�61   8  $ 0:0:0.2D�5=   /   5>      5?       5A      �� 5B      .��5C      �� .��5D   "   �� $1 +(Button.Background).(SolidColorBrush.Color)�61   8  $ 0:0:0.2D�5F   /   5G      5H      5J      5L      �� 5O   
   " ��6&   .�5P      ��  � ��8	   6+   �� 5Q       � ��8	  6+   �� 5R       � ��8	  6,   �� 5S       � ��$	 2q�60   �� 5T       � ��$
	 10,5q�6(   �� 5U       � ��$	 Center=�69   �� 5V       � ��	 5W      �� 5X      "
 ��6&   .R�5Y      �� #��z��6!   #��z��5Z   !   #��z��5[   !   $ 8��5\   !   ._�5]      �� $ PART_ContentHost��6+   5^      5_      5a       5b      P� 5c       �@J�	IsFocused . . �5d      ��  � ��8	  64   5e      5g      �� 5j   
   " G�6*   .�5k      ��  � ��8	   6+   �� 5l       � ��8	  6+   �� 5m       � ��8	  6,   �� 5n       � ��$	 2q�60   �� 5o       � ��$
	 10,5q�6(   �� 5p       � ��$	 Center=�69   �� 5q       � ��	 5r      �� 5s      "
 G�6&   .R�5t      �� #��z��6!   #��z��5u   !   #��z��5v   !   $ 8��5w   !   ._�5x      �� $ PART_ContentHost��6+   5y      5z      5|       5}      P� 5~       �@J�	IsFocused . . �5      ��  � ��8	  64   5�      5�      5�      .��5�      �� #��[ 6   $ 20��5�      $��1q�5�      #��[ 5�      ._�5�   
   �  � RowDefinitions 5�      �� 5�      $��30 �6    �� 5�      $��* �6    .+�5�      � -   5        $��0��5�      
������ .+�5�      �� $F��@Ветеринарная Клиника - Авторизация��6   #��[
 5�      $��10,0,0,0q�5�      $��Center=�5�      �� 5�       �� Orientation$ 
Horizontal=�6   $��Right=�5�      $��0,0,5,0q�5�      .+�5�      �� -   5         ��Name$ MinimizeButton��5�      $	��—��5�      #��[ 5�      $��30��5�      $��25��5�      �� 5�      -   5        $ MaximizeButton��5�      $	��□��5�      #��[ 5�      $��30��5�      $��25��5�      $	��5,0q�5�      �� 5�      -   5        $ CloseButton��5�      $	��✕��5�      #��[ 5�      $��30��5�      $��25��5�      5�      5�      � 5�      $��1��6    � ColumnDefinitions 5�      �� 5�      $��* �6'   �� 5�      $	��400 �6'   .+�5�      � $��0��6   .+�5�      �� $j��dhttps://img.freepik.com/free-photo/veterinarian-checking-cute-dog-with-stethoscope_23-2149143877.jpg��6   $��UniformToFill=�5�       J� Effect 5�      -  'System.Windows.Media.Effects.BlurEffect  5�         Radius$ 2Q�6)   5�      �� 5�      -   5         ��Name$ AnimatedGrid��5�      }�. 5�      �� 5�      $��Center=�6!   $��Center=�5�   !   $��20q�5�   !   .+�5�      �� $%��Добро пожаловать��6$   $��42(�5�   $   $
��Bold"�5�   $   
������� �� TextAlignment$ Center=�5�   $   �� 5�      $0��*в ветеринарную клинику��6$   $��32(�5�   $   
�������$ Center=�5�   $   $��0,5,0,25q�5�   $   �� 5�      $]��WЗдоровье ваших питомцев - наш главный приоритет��6$   $��18(�5�   $   
�������$ Center=�5�   $   $
��Wrap=�5�   $   5�      5�      �� 5�      $��1��6   #��[	 5�      $ 	20,0,0,20��5�      $��0q�5�      ._�5�      �� $��40q�6!   $��Center=�5�   !   .+�5�      �� $=��7https://cdn-icons-png.flaticon.com/512/2138/2138508.png��6    $��80��5�       $��80��5�       $��0,0,0,20q�5�       �� 5�      $��Авторизация��6$   $��32(�5�   $   $
��Bold"�5�   $   #��[
 5�   $   $��0,0,0,40q�5�   $   �� 5�      $��Логин или Email��6$   #��[
 5�   $   $��0,0,0,5q�5�   $   �� 5�      -   5         ��Name$ LoginTextBox��5�   "   $��40��5�   "   $��16(�5�   "   #��[ 5�   "   �� 5�      $��Пароль��6$   #��[
 5�   $   $��0,20,0,5q�5�   $   G� 5�      -   5         G�Name$ PasswordBox��5�   &   $��40��5�   &   $��16(�5�   &   #��[ 5�   &   �� 5�      -   5        $��
Войти��5�   !   #��[ 5�   !   $��0,40,0,0q�5�   !   $��45��5�   !   $��16(�5�   !   5   !   �� 5     $��0,20,0,0q�6$   $��Center=�5  $   �� 5     -	   5         ��Name$ RegisterLink��5  (   #��[ 5  (     �� TextDecorations$
  None{�5  (   .�5  C   .�5  "   �� $*��$Зарегистрироваться��6,   $��14(�5	  ,   5
     5     5     5     5     5     5     5     